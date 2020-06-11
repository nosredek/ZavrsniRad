import Vue from 'vue';
import Vuex from 'vuex';
Vue.use(Vuex);
const store = {
    state: {
        isLoading: false,
        alignmentFilePath: null,
        referenceFilePath: null,
        references: [],
        loadingMessage: "loading"
    },
    getters: {
        isLoadingInProgress(state) {
            return state.isLoading;
        },
        references(state) {
            return state.references;
        },
        alignmentFilePath(state) {
            return state.alignmentFilePath;
        },
        referenceFilePath(state) {
            return state.referenceFilePath;
        },
        loadingMessage(state) {
            return state.loadingMessage;
        }
    },
    mutations: {
        SET_IsLoading(state, valueToSet) {
            state.isLoading = valueToSet;
        },
        SET_AlignmentFilePath(state, valueToSet) {
            state.alignmentFilePath = valueToSet;
        },
        SET_ReferenceFilePath(state, valueToSet) {
            state.referenceFilePath = valueToSet;
        },
        SET_References(state, valueToSet) {
            state.references = valueToSet;
        }
    },
    actions: {
        setAlignmentFilePath({ commit }, alignmentFilePath) {
            commit('SET_AlignmentFilePath', alignmentFilePath);
        },
        setReferenceFilePath({ commit }, referenceFilePath) {
            commit('SET_ReferenceFilePath', referenceFilePath);
        },
        setReferences({ commit }, references) {
            commit('SET_References', references);
        },
        loadingInProgress({ commit }, isLoading) {
            commit('SET_IsLoading', isLoading);
        }
    }
};
export default new Vuex.Store(store);
//# sourceMappingURL=store.js.map