export interface RootState {
    isLoading: boolean
    references: Array<ReferenceModel>
    referenceFilePath: string | null
    alignmentFilePath: string | null
    loadingMessage: string | null
}

export interface ReferenceModel{
    name: string
    length: number
}