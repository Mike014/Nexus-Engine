import { useState, useEffect, useCallback } from 'react'
import {
  HubConnectionBuilder,
  HubConnectionState,
  type HubConnection,
} from '@microsoft/signalr'

export interface TradeData {
  buyOrderId: string
  sellOrderId: string
  price: number
  quantity: number
  executedAt: string
}

export interface OrderBookLevel {
  price: number
  quantity: number
}

export interface OrderBookData {
  symbol: string
  bids: OrderBookLevel[]
  asks: OrderBookLevel[]
}

export interface BalanceData {
  accountId: string
  balance: number
  reservedBalance: number
}

export interface NexusHubState {
  trades: TradeData[]
  orderBook: OrderBookData | null
  balanceUpdate: BalanceData | null
  connected: boolean
}

const HUB_URL = `${import.meta.env.VITE_API_URL ?? "http://localhost:5000"}/hubs/nexus`

export function useNexusHub(): NexusHubState {
  const [trades, setTrades] = useState<TradeData[]>([])
  const [orderBook, setOrderBook] = useState<OrderBookData | null>(null)
  const [balanceUpdate, setBalanceUpdate] = useState<BalanceData | null>(null)
  const [connected, setConnected] = useState(false)

  const onTradesExecuted = useCallback(
    (data: TradeData[]) => {
      setTrades((prev) => [...data, ...prev].slice(0, 10))
    },
    [],
  )

  const onOrderBookSnapshot = useCallback((data: OrderBookData) => {
    setOrderBook(data)
  }, [])

  const onBalanceChanged = useCallback((data: BalanceData) => {
    setBalanceUpdate(data)
  }, [])

  useEffect(() => {
    let connection: HubConnection

    async function connect() {
      connection = new HubConnectionBuilder()
        .withUrl(HUB_URL)
        .withAutomaticReconnect()
        .build()

      connection.on('TradesExecuted', onTradesExecuted)
      connection.on('OrderBookSnapshot', onOrderBookSnapshot)
      connection.on('BalanceChanged', onBalanceChanged)

      connection.onreconnecting(() => setConnected(false))
      connection.onreconnected(() => setConnected(true))
      connection.onclose(() => setConnected(false))

      try {
        await connection.start()
        setConnected(true)
      } catch {
        setConnected(false)
      }
    }

    connect()

    return () => {
      if (
        connection &&
        connection.state !== HubConnectionState.Disconnected
      ) {
        connection.stop()
      }
    }
  }, [onTradesExecuted, onOrderBookSnapshot, onBalanceChanged])

  return { trades, orderBook, balanceUpdate, connected }
}
