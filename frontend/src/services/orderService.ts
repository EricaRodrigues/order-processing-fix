import { OrderRequest, OrderResponse } from '../types/order'

const API_URL = '/api/orders'

export async function sendOrder(order: OrderRequest): Promise<OrderResponse> {
    const response = await fetch(API_URL, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(order),
    })

    if (!response.ok) {
        const error = await response.json()
        throw new Error(error.message ?? 'Unexpected error')
    }

    return response.json()
}
