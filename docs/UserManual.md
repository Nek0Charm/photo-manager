# 使用手册


## 1. 环境要求

| 组件 | 版本 | 说明 |
| --- | --- | --- |
| Node.js | ≥ 18 | 运行前端（推荐配合 `pnpm`） |
| pnpm | ≥ 8 | 安装与启动 Vue 前端 |
| .NET SDK | 9.0 | 运行 ASP.NET Core 后端 |
| SQLite | – | 随仓库附带，无需单独安装 |
| Docker & Compose | 可选 | 一键部署整套服务 |

> 若需启用 AI 标签，请准备可访问 OpenAI Vision 的 API Key。

---

## 2. 本地开发部署

1. **克隆仓库并进入根目录**
   ```bash
   git clone git@github.com:Nek0Charm/photo-manager.git
   cd photo-manager
   ```
2. **启动后端**
   ```bash
   cd backend
   dotnet restore
   dotnet watch run
   ```
   - 默认监听 <https://localhost:5151>（或 `appsettings.Development.json` 中配置的端口）。
   - 首次启动会自动创建 `app.db` 和 `wwwroot/uploads`。
3. **启动前端**
   ```bash
   cd frontend
   pnpm install
   pnpm run dev
   ```
   - 默认访问 <http://localhost:5173>

### 2.1 Docker 部署

```bash
docker-compose up -d --build
```

- 前端经 Nginx 暴露 `http://localhost:8080`。
- 后端暴露 `http://localhost:5000`，`/api` 已由 Nginx 反代。
- 数据卷：
  - `./backend/app.db:/app/app.db`
  - `./backend/wwwroot/uploads:/app/wwwroot/uploads`

---

## 3. 登录与账号

1. 打开前端页面，若未登录将看到“登录 / 注册”切换面板。
2. **注册**：填写用户名、邮箱、密码后提交，需保持唯一。
3. **登录**：可使用“用户名或邮箱 + 密码”，成功后顶部头像菜单显示当前用户。
4. 页面刷新时会自动调用 `/api/auth/me`，若 Session 失效请重新登录。

---

## 4. 照片管理流程

### 4.1 上传

1. 登录后点击右下角 `UploadFab` 漂浮按钮。
2. 选择 1~n 张（单张 ≤ 50MB）的图片，可一次性填写描述、手动标签、拍摄时间、地点。
3. 提交后：
   - 原图与 512px 缩略图分别保存至 `uploads/original` 与 `uploads/thumbs`。
   - 系统解析 EXIF 并自动生成相应标签。
   - 任务进入 AI 标签队列，稍后自动补全。
4. 上传成功会触发列表刷新并弹出 Snackbar 提示。

### 4.2 浏览与筛选

- 顶部搜索框支持关键词模糊匹配描述、地点、文件名。
- 左侧标签栏可多选标签；日期区间与排序（创建时间/拍摄时间，正序或倒序）位于筛选面板。
- `PhotoGallery` 中点击缩略图进入 `PhotoViewer` 灯箱，可左右切换。

### 4.3 编辑与元数据

| 操作 | 入口 | 说明 |
| --- | --- | --- |
| 图像编辑 | `PhotoViewer` → “编辑” | 打开 `PhotoEditor`，可裁剪、旋转、调节亮度/对比度/饱和度/灰度，并选择覆盖或“另存为新图”。 |
| 描述/标签 | `PhotoViewer` → “编辑标签” | `PhotoMetadataDialog` 中增删手动标签、更新描述；不会影响 EXIF/AI 标签。 |
| 删除 | `PhotoViewer` → “删除” 或 Gallery 批量模式 | `PhotoDeleteDialog` 确认后，数据库记录与两份物理文件都会移除。 |

批量删除：在 `PhotoGallery` 中启用批量模式，选择多张照片后逐条调用删除接口。

---

## 5. 标签体系

| 类型 | 来源 | 查看/维护方式 |
| --- | --- | --- |
| 手动 (`Manual`) | 上传或编辑时输入 | `PhotoMetadataDialog` 可增删 |
| EXIF (`Exif`) | 拍摄日期、年月、GPS、机型 | 自动生成，只读 |
| AI (`Ai`) | `AiTaggingBackgroundService` 调用 OpenAI Vision | 当重新生成 AI 标签时会清空旧数据再写入 |

- `PhotoViewer` 中的标签按类型着色，鼠标悬浮可见类型提示。
- AI 标签生成失败时，可在“AI 设置”对话框核对模型/Key 是否配置。

---

## 6. AI 标签配置

1. 登录后点击标题栏头像 → “AI 标签设置”。
2. 在 `AiSettingsDialog` 中选择模型（默认 OpenAI `gpt-4.1-mini`），可选自定义 Endpoint。
3. 首次保存或勾选“更新 API Key”时需要输入 Key，存储于服务器的 `UserAiSettings.ApiKey`，不会回显。
4. 上传/编辑照片后后台任务会读取该配置调用 OpenAI Vision，生成 1~3 个中文标签。

---

## 7. MCP 检索接口

系统同时暴露 **SSE MCP Server**（推荐 VS Code、Claude Desktop 等客户端使用）与传统的 `POST /api/mcp/search` REST 端点。二者共用同一套自然语言检索逻辑。

### 7.1 SSE 工具模式（VS Code / Claude Desktop）

1. **先登录并获得 Session Cookie**
   ```bash
   curl -c cookies.txt -X POST http://localhost:5151/api/auth/login \
     -H "Content-Type: application/json" \
     -d '{"username":"demo","password":"pass"}'
   ```
   - 将 `cookies.txt` 中 `.AspNetCore.Session=...` 的值复制出来，写入 VS Code `mcp-servers.json`（或 Claude Desktop 的 `claude_desktop_config.json`）的 `headers.Cookie` 字段。
2. **配置客户端指向 `/mcp/sse`**
   ```json
   {
     "mcpServers": {
       "photo-manager": {
         "url": "http://localhost:5151/mcp/sse",
         "transport": "sse",
         "headers": {
           "Cookie": ".AspNetCore.Session=CfDJ..."
         }
       }
     }
   }
   ```
   - `MapMcp()` 会自动创建 `/mcp/sse`（握手）和 `/mcp/messages`（请求响应）两个路由。
3. **可用工具**

   | 工具 ID | 参数 | 说明 |
   | --- | --- | --- |
   | `search_gallery_photos` | `query`(必填), `limit?`, `from?`, `to?`, `filters?` | 返回符合 MCP 规范的搜索结果列表，供 AI 直接使用。 |
   | `get_photo_details` | `photoId` | 返回指定照片的尺寸、时间、地点与标签等元数据。 |

> 注意：MCP 工具同样依赖 Session，过期后需重新登录并更新配置中的 Cookie。

---

## 8. 常见问题排查

| 问题 | 建议检查 |
| --- | --- |
| 前端跨域/无法登录 | 确认后端 `appsettings.Development.json` 的 `AllowedOrigins` 包含前端地址；请求需携带 Session Cookie (`withCredentials: true`)。 |
| 上传失败或超时 | 核实文件大小是否超过 50MB，或查看后端日志中 `ImageService` 的异常。 |
| AI 标签长时间未生成 | 确保后台进程（`dotnet watch run` 或 Docker 后端容器）持续运行，并在 AI 设置中填入有效的 API Key/模型。 |
| MCP 返回空结果 | 当前查询用户仅能检索自己的照片，确认 Session 是否有效及时间范围/关键词是否匹配。 |

---
