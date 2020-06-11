import Vue from 'vue'
import Vuex, { StoreOptions } from 'vuex'
import { RootState, ReferenceModel } from '@/store/types'

Vue.use(Vuex);


const store: StoreOptions<RootState> = {
    state: {
        isLoading: false,
        alignmentFilePath: null,
        referenceFilePath: null,
        references: [],
        loadingMessage: "loading"
    },

    getters: {
        isLoadingInProgress(state): boolean {
            return state.isLoading
        },
        references(state): Array<ReferenceModel> {
            return state.references
        },
        alignmentFilePath(state): string | null {
            return state.alignmentFilePath
        },
        referenceFilePath(state): string | null {
            return state.referenceFilePath
        }
        ,
        loadingMessage(state): string | null {
            return state.loadingMessage
        }
    },

    mutations: {
        SET_IsLoading(
            state,
            valueToSet: boolean
        ) {
            state.isLoading = valueToSet
        },
        SET_AlignmentFilePath(
            state,
            valueToSet: string
        ) {
            state.alignmentFilePath = valueToSet
        },
        SET_ReferenceFilePath(
            state,
            valueToSet: string
        ) {
            state.referenceFilePath = valueToSet
        },
        SET_References(
            state,
            valueToSet: Array<ReferenceModel>
        ) {
            state.references = valueToSet
        }
    },

    actions: {
        setAlignmentFilePath(
            { commit },
            alignmentFilePath: string
        ): void {
            commit('SET_AlignmentFilePath', alignmentFilePath)
        },
        setReferenceFilePath(
            { commit },
            referenceFilePath: string
        ): void {
            commit('SET_ReferenceFilePath', referenceFilePath)
        },
        setReferences(
            { commit },
            references: Array<ReferenceModel>
        ): void {
            commit('SET_References', references)
        },
        loadingInProgress(
            { commit },
            isLoading: boolean
        ): void {
            commit('SET_IsLoading', isLoading)
        }
    }
}

export default new Vuex.Store<RootState>(store)