export interface SamFile{
    queryName: string;
    referenceName: string;
    startingPos: number;
    drawExpressions: Array<DrawExpression>;
    reverse: boolean
}

export interface DrawExpression{
    length:number;
    type: string;
}