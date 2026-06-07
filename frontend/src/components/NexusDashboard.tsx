import { useState, useEffect, useCallback } from 'react'
import { useNexusHub } from '../hooks/useNexusHub'
import {
  createAccount,
  deposit,
  placeOrder,
  cancelOrder,
  getOrders,
  type OrderData,
} from '../api/nexusApi'

const row: React.CSSProperties = {
  display: 'flex',
  gap: 24,
  flexWrap: 'wrap',
}

const panel: React.CSSProperties = {
  flex: '1 1 300px',
  border: '1px solid var(--border)',
  borderRadius: 8,
  padding: 16,
  background: 'var(--code-bg)',
}

const table: React.CSSProperties = {
  width: '100%',
  borderCollapse: 'collapse',
  fontSize: 13,
}

const th: React.CSSProperties = {
  textAlign: 'left',
  borderBottom: '1px solid var(--border)',
  padding: '4px 6px',
  color: 'var(--text)',
}

const td: React.CSSProperties = {
  padding: '4px 6px',
  borderBottom: '1px solid var(--border)',
  color: 'var(--text)',
}

const badge: React.CSSProperties = {
  display: 'inline-block',
  width: 10,
  height: 10,
  borderRadius: '50%',
  marginRight: 6,
}

const input: React.CSSProperties = {
  display: 'block',
  width: '100%',
  boxSizing: 'border-box',
  marginBottom: 6,
  padding: '4px 6px',
  fontSize: 13,
  border: '1px solid var(--border)',
  borderRadius: 4,
  background: 'var(--bg)',
  color: 'var(--text)',
}

const btn: React.CSSProperties = {
  padding: '4px 12px',
  fontSize: 13,
  cursor: 'pointer',
  border: '1px solid var(--border)',
  borderRadius: 4,
  background: 'var(--code-bg)',
  color: 'var(--text)',
  marginBottom: 4,
}

const feedback: React.CSSProperties = {
  fontSize: 12,
  marginTop: 4,
  wordBreak: 'break-all',
}

type Feedback = { ok: boolean; msg: string } | null

function FormPanel({
  title,
  children,
}: {
  title: string
  children: React.ReactNode
}) {
  return (
    <div style={panel}>
      <h4 style={{ margin: '0 0 8px', color: 'var(--text-h)' }}>{title}</h4>
      {children}
    </div>
  )
}

function Input({
  value,
  onChange,
  placeholder,
  type,
}: {
  value: string
  onChange: (v: string) => void
  placeholder?: string
  type?: string
}) {
  return (
    <input
      style={input}
      value={value}
      onChange={(e) => onChange(e.target.value)}
      placeholder={placeholder}
      type={type ?? 'text'}
    />
  )
}

function Feedback({ fb }: { fb: Feedback }) {
  if (!fb) return null
  return (
    <div style={{ ...feedback, color: fb.ok ? '#16a34a' : '#dc2626' }}>
      {fb.msg}
    </div>
  )
}

const sidebarWidth = 400
const sidebarBorder = '1px solid var(--border)'

const sectionTitle: React.CSSProperties = {
  fontSize: 13,
  fontWeight: 700,
  textTransform: 'uppercase',
  letterSpacing: '1px',
  color: 'var(--accent)',
  margin: '24px 0 10px',
  paddingBottom: 4,
  borderBottom: sidebarBorder,
  textAlign: 'center',
}

const conceptBlock: React.CSSProperties = {
  marginBottom: 20,
  fontSize: 16,
  lineHeight: 1.6,
  color: 'var(--text)',
  paddingLeft: 12,
}

const conceptTitle: React.CSSProperties = {
  display: 'block',
  fontWeight: 600,
  color: 'var(--text-h)',
  marginBottom: 2,
}

const formulaBox: React.CSSProperties = {
  background: 'var(--bg)',
  border: sidebarBorder,
  borderRadius: 4,
  padding: '4px 12px',
  margin: '4px 0',
  fontFamily: 'ui-monospace, Consolas, monospace',
  fontSize: 11,
  color: 'var(--text-h)',
  textAlign: 'left',
}

const stepList: React.CSSProperties = {
  margin: 0,
  paddingLeft: 24,
  fontSize: 12,
  lineHeight: 1.8,
  color: 'var(--text)',
  overflowWrap: 'break-word',
}

const eventCode: React.CSSProperties = {
  fontFamily: 'ui-monospace, Consolas, monospace',
  fontSize: 11,
  color: 'var(--text-h)',
  background: 'var(--bg)',
  borderRadius: 3,
  padding: '1px 4px',
}

const dataRow: React.CSSProperties = {
  display: 'flex',
  justifyContent: 'space-between',
  fontSize: 12,
  padding: '3px 0',
  borderBottom: sidebarBorder,
  color: 'var(--text)',
}

const dataLabel: React.CSSProperties = {
  fontWeight: 500,
  color: 'var(--text-h)',
}

const sidebarLink: React.CSSProperties = {
  color: 'var(--accent)',
  textDecoration: 'none',
  fontFamily: 'ui-monospace, Consolas, monospace',
  fontSize: 11,
}

function GuideSidebar() {
  return (
    <div
      style={{
        width: sidebarWidth,
        minWidth: sidebarWidth,
        padding: '12px 16px',
        overflowY: 'auto',
        overflowX: 'hidden',
        boxSizing: 'border-box',
        overflowWrap: 'break-word',
        wordBreak: 'break-word',
      }}
    >
      <h2
        style={{
          fontSize: 15,
          fontWeight: 700,
          margin: '0 0 4px',
          color: 'var(--text-h)',
          textAlign: 'left',
        }}
      >
        Nexus Guide
      </h2>
      <p style={{ fontSize: 11, color: 'var(--text)', marginBottom: 8, textAlign: 'left' }}>
        Simulated order book exchange engine
      </p>

      <div style={sectionTitle}>How to Use</div>
      <ol style={stepList}>
        <li>
          <strong style={{ color: 'var(--text-h)' }}>Create Account</strong> — no input needed, just click
        </li>
        <li>
          <strong style={{ color: 'var(--text-h)' }}>Deposit funds</strong> — min required = price × quantity
        </li>
        <li>
          <strong style={{ color: '#16a34a' }}>Place Buy order</strong> — you want to buy BTC
        </li>
        <li>
          <strong style={{ color: '#dc2626' }}>Place Sell order</strong> from another account (triggers match)
        </li>
        <li>
          Watch <span style={eventCode}>Order Book</span>,{' '}
          <span style={eventCode}>Recent Trades</span>,{' '}
          <span style={eventCode}>Balance</span> update in real time
        </li>
        <li>
          <strong style={{ color: 'var(--text-h)' }}>Cancel</strong> a pending order to release reserved balance
        </li>
      </ol>

      <div style={sectionTitle}>Technical Concepts</div>

      <div style={conceptBlock}>
        <span style={conceptTitle}>Price-Time Priority FIFO</span>
        <div style={formulaBox}>best price wins, ties broken by arrival time</div>
        <div>Buy orders sorted descending by price (highest bid first)</div>
        <div>Sell orders sorted ascending by price (lowest ask first)</div>
      </div>

      <div style={conceptBlock}>
        <span style={conceptTitle}>Balance Reservation</span>
        <div style={formulaBox}>Reserved = Price × Quantity</div>
        <div style={formulaBox}>Available = Total Balance − Reserved</div>
        <div>Order rejected if Available &lt; Price × Quantity</div>
      </div>

      <div style={conceptBlock}>
        <span style={conceptTitle}>Partial Fill</span>
        <div>A Buy of qty 0.5 matched by a Sell of qty 0.2</div>
        <div>Results in: Trade qty 0.2, Buy residual qty 0.3 stays in book</div>
      </div>

      <div style={conceptBlock}>
        <span style={conceptTitle}>Optimistic Concurrency</span>
        <div>Each event has <span style={eventCode}>aggregate_version</span> (sequential integer)</div>
        <div>
          <span style={eventCode}>UNIQUE</span> constraint on{' '}
          <span style={eventCode}>(aggregate_id, aggregate_version)</span>
        </div>
        <div>On conflict: retry up to 3 times with 50-300ms jitter</div>
      </div>

      <div style={conceptBlock}>
        <span style={conceptTitle}>Event Sourcing</span>
        <div>Every state change appended to{' '}
          <span style={eventCode}>domain_events</span> table</div>
        <div>Account/Order state reconstructed by replaying events</div>
        <div>
          Replay endpoint:{' '}
          <span style={sidebarLink}>GET /api/accounts/&#123;id&#125;/replay</span>
        </div>
      </div>

      <div style={conceptBlock}>
        <span style={conceptTitle}>SignalR Events</span>
        <div style={{ marginTop: 2 }}>
          <span style={eventCode}>"TradesExecuted"</span> — list of matched trades
        </div>
        <div>
          <span style={eventCode}>"OrderBookSnapshot"</span> — full bids/asks state
        </div>
        <div>
          <span style={eventCode}>"BalanceChanged"</span> — account balance update
        </div>
      </div>

      <div style={sectionTitle}>Market Data</div>
      <div style={dataRow}>
        <span style={dataLabel}>Symbol</span>
        <span>BTC/USD</span>
      </div>
      <div style={dataRow}>
        <span style={dataLabel}>Price precision</span>
        <span>2 decimals (NUMERIC 18,2)</span>
      </div>
      <div style={dataRow}>
        <span style={dataLabel}>Quantity precision</span>
        <span>8 decimals (NUMERIC 18,8)</span>
      </div>
      <div style={{ ...dataRow, borderBottom: 'none' }}>
        <span style={dataLabel}>Order types</span>
        <span>Limit only (Phase 3)</span>
      </div>
    </div>
  )
}

export default function NexusDashboard() {
  const { trades, orderBook, balanceUpdate, connected } = useNexusHub()
  const [isMobile, setIsMobile] = useState(false)
  const [showGuide, setShowGuide] = useState(false)

  useEffect(() => {
    const mq = window.matchMedia('(max-width: 768px)')
    setIsMobile(mq.matches)
    const handler = (e: MediaQueryListEvent) => setIsMobile(e.matches)
    mq.addEventListener('change', handler)
    return () => mq.removeEventListener('change', handler)
  }, [])

  const [accRes, setAccRes] = useState<Feedback>(null)
  const [depAcc, setDepAcc] = useState('')
  const [depAmt, setDepAmt] = useState('')
  const [depRes, setDepRes] = useState<Feedback>(null)
  const [plAcc, setPlAcc] = useState('')
  const [plSide, setPlSide] = useState('Buy')
  const [plPrice, setPlPrice] = useState('')
  const [plQty, setPlQty] = useState('')
  const [plRes, setPlRes] = useState<Feedback>(null)
  const [cnOrder, setCnOrder] = useState('')
  const [cnAcc, setCnAcc] = useState('')
  const [cnRes, setCnRes] = useState<Feedback>(null)
  const [goAcc, setGoAcc] = useState('')
  const [goOrders, setGoOrders] = useState<OrderData[] | null>(null)
  const [goRes, setGoRes] = useState<Feedback>(null)

  const handleCreateAccount = useCallback(async () => {
    setAccRes(null)
    try {
      const id = await createAccount()
      setAccRes({ ok: true, msg: `Account created: ${id}` })
    } catch (e) {
      setAccRes({ ok: false, msg: String(e) })
    }
  }, [])

  const handleDeposit = useCallback(async () => {
    setDepRes(null)
    try {
      await deposit(depAcc, Number(depAmt))
      setDepRes({ ok: true, msg: 'Deposit successful' })
    } catch (e) {
      setDepRes({ ok: false, msg: String(e) })
    }
  }, [depAcc, depAmt])

  const handlePlaceOrder = useCallback(async () => {
    setPlRes(null)
    try {
      const id = await placeOrder(plAcc, plSide, Number(plPrice), Number(plQty))
      setPlRes({ ok: true, msg: `Order placed: ${id}` })
    } catch (e) {
      setPlRes({ ok: false, msg: String(e) })
    }
  }, [plAcc, plSide, plPrice, plQty])

  const handleCancelOrder = useCallback(async () => {
    setCnRes(null)
    try {
      await cancelOrder(cnOrder, cnAcc)
      setCnRes({ ok: true, msg: 'Order cancelled' })
    } catch (e) {
      setCnRes({ ok: false, msg: String(e) })
    }
  }, [cnOrder, cnAcc])

  const handleGetOrders = useCallback(async () => {
    setGoOrders(null)
    setGoRes(null)
    try {
      const list = await getOrders(goAcc)
      setGoOrders(list)
      setGoRes({ ok: true, msg: `Found ${list.length} orders` })
    } catch (e) {
      setGoRes({ ok: false, msg: String(e) })
    }
  }, [goAcc])

  const mainContent = (
    <div style={{ flex: 1, minWidth: 0 }}>
      <div style={{ marginBottom: 16 }}>
        <span
          style={{ ...badge, background: connected ? '#22c55e' : '#ef4444' }}
        />
        <strong style={{ color: 'var(--text-h)' }}>
          {connected ? 'Connected' : 'Disconnected'}
        </strong>
      </div>

      <div style={row}>
        <div style={panel}>
          <h3 style={{ marginTop: 0, color: 'var(--text-h)' }}>Order Book</h3>
          {orderBook ? (
            <table style={table}>
              <thead>
                <tr>
                  <th style={th}>Price</th>
                  <th style={th}>Quantity</th>
                </tr>
              </thead>
              <tbody>
                {orderBook.asks.slice(0, 5).map((l, i) => (
                  <tr key={`ask-${l.price}-${i}`}>
                    <td style={{ ...td, color: '#dc2626' }}>{l.price}</td>
                    <td style={td}>{l.quantity}</td>
                  </tr>
                ))}
              </tbody>
              <tfoot>
                <tr>
                  <td style={{ ...td, fontWeight: 700, paddingTop: 8 }}>
                    {orderBook.symbol}
                  </td>
                  <td style={td} />
                </tr>
              </tfoot>
              <tbody>
                {orderBook.bids.slice(0, 5).map((l, i) => (
                  <tr key={`bid-${l.price}-${i}`}>
                    <td style={{ ...td, color: '#16a34a' }}>{l.price}</td>
                    <td style={td}>{l.quantity}</td>
                  </tr>
                ))}
              </tbody>
            </table>
          ) : (
            <p style={{ color: '#888' }}>Waiting for snapshot…</p>
          )}
        </div>

        <div style={panel}>
          <h3 style={{ marginTop: 0, color: 'var(--text-h)' }}>Recent Trades</h3>
          {trades.length > 0 ? (
            <table style={table}>
              <thead>
                <tr>
                  <th style={th}>Price</th>
                  <th style={th}>Qty</th>
                  <th style={th}>Time</th>
                </tr>
              </thead>
              <tbody>
                {trades.map((t, i) => (
                  <tr key={`${t.executedAt}-${i}`}>
                    <td style={td}>{t.price}</td>
                    <td style={td}>{t.quantity}</td>
                    <td style={td}>
                      {new Date(t.executedAt).toLocaleTimeString()}
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          ) : (
            <p style={{ color: '#888' }}>No trades yet…</p>
          )}
        </div>

        <div style={panel}>
          <h3 style={{ marginTop: 0, color: 'var(--text-h)' }}>Balance Update</h3>
          {balanceUpdate ? (
            <dl style={{ margin: 0, lineHeight: 1.8 }}>
              <dt style={{ fontSize: 12, color: 'var(--text)' }}>Account</dt>
              <dd style={{ margin: '0 0 8px', fontFamily: 'monospace', color: 'var(--text-h)' }}>
                {balanceUpdate.accountId}
              </dd>
              <dt style={{ fontSize: 12, color: 'var(--text)' }}>Balance</dt>
              <dd style={{ margin: '0 0 8px', color: 'var(--text-h)' }}>{balanceUpdate.balance}</dd>
              <dt style={{ fontSize: 12, color: 'var(--text)' }}>Reserved</dt>
              <dd style={{ margin: 0, color: 'var(--text-h)' }}>{balanceUpdate.reservedBalance}</dd>
            </dl>
          ) : (
            <p style={{ color: '#888' }}>No balance update yet…</p>
          )}
        </div>
      </div>

      <h2 style={{ marginTop: 32, fontSize: 18, color: 'var(--text-h)' }}>Actions</h2>

      <div style={row}>
        <FormPanel title="Create Account">
          <button style={btn} onClick={handleCreateAccount}>
            Create Account
          </button>
          <Feedback fb={accRes} />
        </FormPanel>

        <FormPanel title="Deposit">
          <Input
            value={depAcc}
            onChange={setDepAcc}
            placeholder="Account ID"
          />
          <Input
            value={depAmt}
            onChange={setDepAmt}
            placeholder="Amount"
            type="number"
          />
          <button style={btn} onClick={handleDeposit}>
            Deposit
          </button>
          <Feedback fb={depRes} />
        </FormPanel>

        <FormPanel title="Place Order">
          <Input
            value={plAcc}
            onChange={setPlAcc}
            placeholder="Account ID"
          />
          <select
            style={input}
            value={plSide}
            onChange={(e) => setPlSide(e.target.value)}
          >
            <option value="Buy">Buy</option>
            <option value="Sell">Sell</option>
          </select>
          <Input
            value={plPrice}
            onChange={setPlPrice}
            placeholder="Price"
            type="number"
          />
          <Input
            value={plQty}
            onChange={setPlQty}
            placeholder="Quantity"
            type="number"
          />
          <button style={btn} onClick={handlePlaceOrder}>
            Place Order
          </button>
          <Feedback fb={plRes} />
        </FormPanel>
      </div>

      <div style={{ ...row, marginTop: 24 }}>
        <FormPanel title="Cancel Order">
          <Input
            value={cnOrder}
            onChange={setCnOrder}
            placeholder="Order ID"
          />
          <Input
            value={cnAcc}
            onChange={setCnAcc}
            placeholder="Account ID"
          />
          <button style={btn} onClick={handleCancelOrder}>
            Cancel Order
          </button>
          <Feedback fb={cnRes} />
        </FormPanel>

        <FormPanel title="Get Orders">
          <Input
            value={goAcc}
            onChange={setGoAcc}
            placeholder="Account ID"
          />
          <button style={btn} onClick={handleGetOrders}>
            Get Orders
          </button>
          <Feedback fb={goRes} />
          {goOrders && goOrders.length > 0 && (
            <div style={{ maxHeight: 200, overflowY: 'auto', fontSize: 12 }}>
              {goOrders.map((o) => (
                <div
                  key={o.id}
                  style={{
                    borderBottom: '1px solid var(--border)',
                    padding: '4px 0',
                  }}
                >
                  <strong style={{ color: 'var(--text-h)' }}>{o.side}</strong>{' '}
                  <span style={{ color: 'var(--text)' }}>{o.quantity} @ {o.price}</span>
                  <br />
                  <span style={{ color: 'var(--text)' }}>ID: {o.id}</span>
                  <br />
                  <span style={{ color: 'var(--text)' }}>
                    Status: {o.status} | Filled: {o.filledQuantity} /{' '}
                    {o.quantity}
                  </span>
                </div>
              ))}
            </div>
          )}
          {goOrders && goOrders.length === 0 && (
            <p style={{ color: 'var(--text)', fontSize: 12 }}>No orders found.</p>
          )}
        </FormPanel>
      </div>
    </div>
  )

  if (isMobile) {
    return (
      <div style={{ fontFamily: 'system-ui, sans-serif', color: 'var(--text)' }}>
        <div
          style={{
            padding: 16,
            borderBottom: '1px solid var(--border)',
            display: 'flex',
            justifyContent: 'space-between',
            alignItems: 'center',
            background: 'var(--code-bg)',
          }}
        >
          <h1 style={{ margin: 0, fontSize: 20, color: 'var(--text-h)' }}>
            Nexus Engine
          </h1>
          <button
            style={{
              ...btn,
              marginBottom: 0,
              fontSize: 12,
              whiteSpace: 'nowrap',
            }}
            onClick={() => setShowGuide(!showGuide)}
          >
            {showGuide ? 'Hide Guide' : 'Show Guide'}
          </button>
        </div>
        {showGuide && (
          <div style={{ borderBottom: sidebarBorder }}>
            <GuideSidebar />
          </div>
        )}
        <div style={{ padding: 16 }}>{mainContent}</div>
      </div>
    )
  }

  return (
    <div
      style={{
        display: 'flex',
        fontFamily: 'system-ui, sans-serif',
        color: 'var(--text)',
        minHeight: '100dvh',
      }}
    >
      <div
        style={{
          width: sidebarWidth,
          minWidth: sidebarWidth,
          borderRight: sidebarBorder,
          background: 'var(--code-bg)',
          overflowY: 'auto',
          position: 'sticky',
          top: 0,
          alignSelf: 'flex-start',
          maxHeight: '100dvh',
        }}
      >
        <div style={{ padding: '12px 16px' }}>
          <div
            style={{
              display: 'flex',
              alignItems: 'center',
              justifyContent: 'space-between',
              marginBottom: 4,
            }}
          >
            <h1 style={{ margin: 0, fontSize: 18, color: 'var(--text-h)' }}>
              Nexus Engine
            </h1>
          </div>
          <p style={{ fontSize: 11, color: 'var(--text)', margin: 0 }}>
            Simulated order book exchange
          </p>
        </div>
        <GuideSidebar />
      </div>
      <div style={{ flex: 1, padding: 24, minWidth: 0 }}>
        {mainContent}
      </div>
    </div>
  )
}
