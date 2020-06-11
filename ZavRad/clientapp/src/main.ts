import Vue from 'vue';
import App from './App.vue';
import router from './router';
import store from '@/store/store'
import './../node_modules/bulma/css/bulma.css';
import { library } from '@fortawesome/fontawesome-svg-core'
import {
  faUpload,
  faSpinner,
  faCheck
} from '@fortawesome/free-solid-svg-icons'
import { FontAwesomeIcon } from '@fortawesome/vue-fontawesome'

library.add(faUpload);
library.add(faSpinner);
library.add(faCheck);

Vue.component('font-awesome-icon', FontAwesomeIcon)

Vue.config.productionTip = false;

new Vue({
  router,
  store,
  render: (h) => h(App),
}).$mount('#app');
