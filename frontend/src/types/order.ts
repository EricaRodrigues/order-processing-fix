export type Symbol = 'PETR4' | 'VALE3' | 'VIIA4'
export type Side = 'Buy' | 'Sell'

export interface OrderRequest {
    symbol: Symbol
    side: Side
    quantity: number
    price: number
}

export interface OrderResponse {
    accepted: boolean
    status: string
    message: string
}
