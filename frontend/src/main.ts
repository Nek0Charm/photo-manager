import { createApp } from 'vue'
import { createPinia } from 'pinia'
import '@mdi/font/css/materialdesignicons.css'
import 'vuetify/styles'
import './style.css'
import App from './App.vue'
import vuetify from './plugins/vuetify'

const app = createApp(App)
app.use(createPinia())
app.use(vuetify)
app.mount('#app')
