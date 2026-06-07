const API_BASE = import.meta.env.VITE_API_URL ?? "http://localhost:5000"
const BASE = `${API_BASE}/api`

export interface OrderData {
  id: string
  accountId: string
  symbol: string
  side: string
  price: number
  quantity: number
  remainingQuantity: number
  filledQuantity: number
  status: string
  createdAt: string
}

async function request<T>(url: string, init?: RequestInit): Promise<T> {
  const res = await fetch(`${BASE}${url}`, {
    headers: { 'Content-Type': 'application/json' },
    ...init,
  })
  if (!res.ok) {
    const text = await res.text()
    throw new Error(text || `HTTP ${res.status}`)
  }
  const text = await res.text()
  return text ? (JSON.parse(text) as T) : (undefined as unknown as T)
}

export async function createAccount(): Promise<string> {
  const res = await request<{ id: string }>('/accounts', { method: 'POST' })
  return res.id
}

export function deposit(accountId: string, amount: number): Promise<void> {
  return request<void>(`/accounts/${accountId}/deposit`, {
    method: 'POST',
    body: JSON.stringify({ amount }),
  })
}

export async function placeOrder(
  accountId: string,
  side: string,
  price: number,
  quantity: number,
): Promise<string> {
  const res = await request<{ id: string }>('/orders', {
    method: 'POST',
    body: JSON.stringify({
      accountId,
      side,
      price,
      quantity,
      symbol: 'BTC/USD',
    }),
  })
  return res.id
}

export function cancelOrder(
  orderId: string,
  accountId: string,
): Promise<void> {
  return request<void>(`/orders/${orderId}?accountId=${accountId}`, {
    method: 'DELETE',
  })
}

export function getOrders(accountId: string): Promise<OrderData[]> {
  return request<OrderData[]>(`/orders?accountId=${accountId}`)
}
