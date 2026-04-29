import { useState } from 'react'
import { OrderRequest, OrderResponse, Symbol, Side } from '../types/order'
import { sendOrder } from '../services/orderService'

const SYMBOLS: Symbol[] = ['PETR4', 'VALE3', 'VIIA4']
const SIDES: Side[] = ['Buy', 'Sell']

export function OrderForm() {
    const [form, setForm] = useState<OrderRequest>({
        symbol: 'PETR4',
        side: 'Buy',
        quantity: 1,
        price: 10.0,
    })
    const [response, setResponse] = useState<OrderResponse | null>(null)
    const [loading, setLoading] = useState(false)
    const [error, setError] = useState<string | null>(null)

    const handleSubmit = async (e: React.FormEvent) => {
        e.preventDefault()
        setLoading(true)
        setError(null)
        setResponse(null)

        try {
            const result = await sendOrder(form)
            setResponse(result)
        } catch (err) {
            setError(err instanceof Error ? err.message : 'Unexpected error')
        } finally {
            setLoading(false)
        }
    }

    return (
        <div className="min-h-screen bg-gray-950 flex items-center justify-center p-6">
            <div className="w-full max-w-md">

                {/* Header */}
                <div className="mb-8 text-center">
                    <h1 className="text-3xl font-bold text-white tracking-tight">Order Generator</h1>
                    <p className="text-gray-400 mt-1 text-sm">FIX 4.4 Protocol</p>
                </div>

                {/* Form Card */}
                <form onSubmit={handleSubmit} className="bg-gray-900 rounded-2xl p-6 shadow-xl border border-gray-800 space-y-5">

                    {/* Symbol */}
                    <div>
                        <label className="block text-sm font-medium text-gray-300 mb-1">Symbol</label>
                        <div className="flex gap-2">
                            {SYMBOLS.map(s => (
                                <button
                                    key={s}
                                    type="button"
                                    onClick={() => setForm(f => ({ ...f, symbol: s }))}
                                    className={`flex-1 py-2 rounded-lg text-sm font-semibold transition-all ${
                                        form.symbol === s
                                            ? 'bg-indigo-600 text-white'
                                            : 'bg-gray-800 text-gray-400 hover:bg-gray-700'
                                    }`}
                                >
                                    {s}
                                </button>
                            ))}
                        </div>
                    </div>

                    {/* Side */}
                    <div>
                        <label className="block text-sm font-medium text-gray-300 mb-1">Side</label>
                        <div className="flex gap-2">
                            {SIDES.map(s => (
                                <button
                                    key={s}
                                    type="button"
                                    onClick={() => setForm(f => ({ ...f, side: s }))}
                                    className={`flex-1 py-2 rounded-lg text-sm font-semibold transition-all ${
                                        form.side === s
                                            ? s === 'Buy'
                                                ? 'bg-emerald-600 text-white'
                                                : 'bg-rose-600 text-white'
                                            : 'bg-gray-800 text-gray-400 hover:bg-gray-700'
                                    }`}
                                >
                                    {s}
                                </button>
                            ))}
                        </div>
                    </div>

                    {/* Quantity */}
                    <div>
                        <label className="block text-sm font-medium text-gray-300 mb-1">
                            Quantity <span className="text-gray-500">(max 99,999)</span>
                        </label>
                        <input
                            type="number"
                            min={1}
                            max={99999}
                            value={form.quantity}
                            onChange={e => setForm(f => ({ ...f, quantity: parseInt(e.target.value) || 1 }))}
                            className="w-full bg-gray-800 text-white rounded-lg px-4 py-2.5 border border-gray-700 focus:outline-none focus:border-indigo-500 transition"
                        />
                    </div>

                    {/* Price */}
                    <div>
                        <label className="block text-sm font-medium text-gray-300 mb-1">
                            Price <span className="text-gray-500">(multiple of 0.01, max 999.99)</span>
                        </label>
                        <input
                            type="number"
                            min={0.01}
                            max={999.99}
                            step={0.01}
                            value={form.price}
                            onChange={e => setForm(f => ({ ...f, price: parseFloat(e.target.value) || 0.01 }))}
                            className="w-full bg-gray-800 text-white rounded-lg px-4 py-2.5 border border-gray-700 focus:outline-none focus:border-indigo-500 transition"
                        />
                    </div>

                    {/* Submit */}
                    <button
                        type="submit"
                        disabled={loading}
                        className="w-full py-3 rounded-lg bg-indigo-600 hover:bg-indigo-500 disabled:opacity-50 text-white font-semibold transition-all"
                    >
                        {loading ? 'Sending...' : 'Send Order'}
                    </button>
                </form>

                {/* Response */}
                {response && (
                    <div className={`mt-4 p-4 rounded-xl border text-sm ${
                        response.accepted
                            ? 'bg-emerald-950 border-emerald-700 text-emerald-300'
                            : 'bg-rose-950 border-rose-700 text-rose-300'
                    }`}>
                        <p className="font-bold text-base mb-1">
                            {response.accepted ? '✓ Order Accepted' : '✗ Order Rejected'}
                        </p>
                        <p className="text-xs opacity-80">{response.message}</p>
                    </div>
                )}

                {/* Error */}
                {error && (
                    <div className="mt-4 p-4 rounded-xl border bg-yellow-950 border-yellow-700 text-yellow-300 text-sm">
                        <p className="font-bold mb-1">⚠ Error</p>
                        <p className="text-xs opacity-80">{error}</p>
                    </div>
                )}

            </div>
        </div>
    )
}
