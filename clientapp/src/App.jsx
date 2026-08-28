import React, { useEffect, useState } from 'react'
import { Routes, Route, NavLink, Outlet } from 'react-router-dom'
import { api, fmtMoney, fmtNum, fmtDate, DOCTYPES, DOCSTATUS } from './api'

function Badge({ text, css }) { return <span className={`badge ${css || 'secondary'}`}>{text}</span> }
function Flash({ msg }) { return msg ? <div className={`flash ${msg.ok ? 'ok' : 'err'}`}>{msg.text}</div> : null }
function Modal({ title, onClose, wide, children }) {
  return (
    <div className="modal-bg" onClick={onClose}>
      <div className="modal" style={wide ? { maxWidth: 760 } : undefined} onClick={e => e.stopPropagation()}>
        <div className="row" style={{ marginBottom: 12 }}><h2 style={{ flex: 1, margin: 0 }}>{title}</h2>
          <button className="btn gray sm" style={{ flex: 'none' }} onClick={onClose}>Đóng</button></div>
        {children}
      </div>
    </div>
  )
}
function Field({ label, children }) { return <div style={{ flex: 1 }}><label>{label}</label>{children}</div> }

function Layout() {
  return (
    <>
      <nav className="nav">
        <span className="brand">📦 MiniWMS</span>
        <NavLink to="/" end>Tổng quan</NavLink>
        <NavLink to="/balances">Tồn kho</NavLink>
        <NavLink to="/docs">Phiếu kho</NavLink>
        <NavLink to="/products">Sản phẩm</NavLink>
        <NavLink to="/warehouses">Kho</NavLink>
      </nav>
      <div className="wrap"><Outlet /></div>
    </>
  )
}

function Dashboard() {
  const [d, setD] = useState(null); const [cache, setCache] = useState('')
  useEffect(() => { api.dashboard().then(r => { setD(r.data); setCache(r.cache) }) }, [])
  if (!d) return <p className="muted">Đang tải…</p>
  return (
    <>
      <h1>Tổng quan kho {cache && <span className="pill">cache: {cache}</span>}</h1>
      <div className="grid kpis">
        <div className="kpi"><div className="v">{d.warehouses}</div><div className="l">Kho</div></div>
        <div className="kpi"><div className="v">{d.products}</div><div className="l">Mặt hàng</div></div>
        <div className="kpi"><div className="v">{fmtNum(d.totalOnHand)}</div><div className="l">Tổng tồn (SL)</div></div>
        <div className="kpi"><div className="v" style={{ fontSize: 20, color: 'var(--success)' }}>{fmtMoney(d.inventoryValue)}</div><div className="l">Giá trị tồn</div></div>
        <div className="kpi"><div className="v">{d.postedDocs}</div><div className="l">Phiếu đã ghi sổ</div></div>
        <div className="kpi"><div className="v" style={{ color: d.lowStock ? 'var(--danger)' : undefined }}>{d.lowStock}</div><div className="l">Mặt hàng dưới định mức</div></div>
      </div>
    </>
  )
}

function Balances() {
  const [rows, setRows] = useState([]); const [whs, setWhs] = useState([]); const [wh, setWh] = useState('')
  const load = () => api.balances(wh || null).then(r => setRows(r.data))
  useEffect(() => { load() }, [wh])
  useEffect(() => { api.warehouses().then(r => setWhs(r.data)) }, [])
  const totalValue = rows.reduce((s, r) => s + r.value, 0)
  return (
    <>
      <div className="toolbar"><h1 style={{ margin: 0, flex: 'none' }}>Tồn kho</h1><div className="sp" />
        <select style={{ maxWidth: 220 }} value={wh} onChange={e => setWh(e.target.value)}>
          <option value="">— Tất cả kho —</option>{whs.map(w => <option key={w.id} value={w.id}>{w.name}</option>)}</select></div>
      <div className="card" style={{ padding: 0, overflow: 'auto' }}>
        <table>
          <thead><tr><th>Kho</th><th>Mã</th><th>Tên hàng</th><th>ĐVT</th><th className="right">Tồn</th><th className="right">Định mức</th><th className="right">Giá vốn</th><th className="right">Giá trị</th></tr></thead>
          <tbody>
            {rows.map((b, i) => (
              <tr key={i} style={b.low ? { background: '#fff5f5' } : undefined}>
                <td>{b.warehouse}</td><td>{b.productCode}</td><td>{b.productName}{b.low && <span className="badge danger" style={{ marginLeft: 6 }}>Thấp</span>}</td>
                <td>{b.uom}</td><td className="right"><b>{fmtNum(b.qty)}</b></td><td className="right muted">{b.minStock || '—'}</td>
                <td className="right">{fmtMoney(b.costPrice)}</td><td className="right">{fmtMoney(b.value)}</td>
              </tr>))}
            {rows.length === 0 && <tr><td colSpan={8} className="muted" style={{ padding: 20 }}>Chưa có tồn.</td></tr>}
          </tbody>
          {rows.length > 0 && <tfoot><tr><td colSpan={7} className="right" style={{ fontWeight: 700 }}>TỔNG GIÁ TRỊ TỒN</td><td className="right" style={{ fontWeight: 700, color: 'var(--brand)' }}>{fmtMoney(totalValue)}</td></tr></tfoot>}
        </table>
      </div>
    </>
  )
}

function Docs() {
  const [rows, setRows] = useState([]); const [type, setType] = useState(''); const [status, setStatus] = useState('')
  const [open, setOpen] = useState(null); const [show, setShow] = useState(false)
  const load = () => api.docs(type === '' ? null : Number(type), status === '' ? null : Number(status)).then(r => setRows(r.data))
  useEffect(() => { load() }, [type, status])
  return (
    <>
      <div className="toolbar"><h1 style={{ margin: 0, flex: 'none' }}>Phiếu kho</h1><div className="sp" />
        <select style={{ maxWidth: 150 }} value={type} onChange={e => setType(e.target.value)}><option value="">— Loại —</option>{DOCTYPES.map((s, i) => <option key={i} value={i}>{s}</option>)}</select>
        <select style={{ maxWidth: 150 }} value={status} onChange={e => setStatus(e.target.value)}><option value="">— Trạng thái —</option>{DOCSTATUS.map((s, i) => <option key={i} value={i}>{s}</option>)}</select>
        <button className="btn sm" style={{ flex: 'none' }} onClick={() => setShow(true)}>+ Tạo phiếu</button></div>
      <div className="card" style={{ padding: 0, overflow: 'auto' }}>
        <table>
          <thead><tr><th>Mã</th><th>Loại</th><th>Từ/Đến kho</th><th>Đối tác</th><th className="right">SL</th><th className="right">Giá trị</th><th>Ngày</th><th>Trạng thái</th></tr></thead>
          <tbody>
            {rows.map(d => (
              <tr key={d.id} style={{ cursor: 'pointer' }} onClick={() => setOpen(d.id)}>
                <td>{d.code}</td><td><Badge text={d.typeText} css={d.typeCss} /></td>
                <td>{d.fromWarehouse ? `${d.fromWarehouse}` : ''}{d.fromWarehouse && d.toWarehouse ? ' → ' : ''}{d.toWarehouse || ''}</td>
                <td>{d.partnerName || '—'}</td><td className="right">{fmtNum(d.totalQty)}</td><td className="right">{fmtMoney(d.totalValue)}</td>
                <td>{fmtDate(d.date)}</td><td><Badge text={d.statusText} css={d.statusCss} /></td>
              </tr>))}
            {rows.length === 0 && <tr><td colSpan={8} className="muted" style={{ padding: 20 }}>Không có phiếu.</td></tr>}
          </tbody>
        </table>
      </div>
      {open && <DocDetail id={open} onClose={() => setOpen(null)} onChanged={load} />}
      {show && <DocForm onClose={() => setShow(false)} onSaved={() => { setShow(false); load() }} />}
    </>
  )
}

function DocDetail({ id, onClose, onChanged }) {
  const [d, setD] = useState(null); const [msg, setMsg] = useState(null)
  const load = () => api.doc(id).then(r => setD(r.data))
  useEffect(() => { load() }, [id])
  const flash = (ok, text) => { setMsg({ ok, text }); setTimeout(() => setMsg(null), 2500) }
  const post = async () => { try { const r = await api.postDoc(id); flash(true, r.data.msg); load(); onChanged() } catch (e) { flash(false, e.message) } }
  const cancel = async () => { try { await api.cancelDoc(id); flash(true, 'Đã hủy phiếu.'); load(); onChanged() } catch (e) { flash(false, e.message) } }
  if (!d) return <Modal title="…" onClose={onClose}><p className="muted">Đang tải…</p></Modal>
  const h = d.doc
  return (
    <Modal title={`Phiếu ${h.code}`} onClose={onClose} wide>
      <Flash msg={msg} />
      <div className="row" style={{ marginBottom: 8 }}><Badge text={h.typeText} css={h.typeCss} /><Badge text={h.statusText} css={h.statusCss} />
        <span className="pill" style={{ flex: 'none' }}>{fmtDate(h.date)}</span></div>
      <dl className="dl">
        {h.fromWarehouse && <><dt>Từ kho</dt><dd>{h.fromWarehouse}</dd></>}
        {h.toWarehouse && <><dt>Đến kho</dt><dd>{h.toWarehouse}</dd></>}
        <dt>Đối tác</dt><dd>{h.partnerName || '—'}</dd>
        <dt>Chứng từ gốc</dt><dd>{h.refNo || '—'}</dd>
        <dt>Ghi chú</dt><dd>{h.note || '—'}</dd>
      </dl>
      <div className="section-t">Dòng hàng</div>
      <table>
        <thead><tr><th>Mã</th><th>Tên</th><th className="right">SL</th><th className="right">Đơn giá</th><th className="right">Thành tiền</th></tr></thead>
        <tbody>{d.lines.map((l, i) => (
          <tr key={i}><td>{l.productCode}</td><td>{l.productName}</td><td className="right">{fmtNum(l.quantity)} {l.uom}</td>
            <td className="right">{fmtMoney(l.unitPrice)}</td><td className="right">{fmtMoney(l.lineValue)}</td></tr>))}</tbody>
        <tfoot><tr><td colSpan={4} className="right" style={{ fontWeight: 700 }}>Tổng</td><td className="right" style={{ fontWeight: 700 }}>{fmtMoney(h.totalValue)}</td></tr></tfoot>
      </table>
      {h.status === 0 && (
        <div className="row" style={{ gap: 6, marginTop: 14 }}>
          <button className="btn sm" onClick={post}>Ghi sổ (cập nhật tồn)</button>
          <button className="btn gray sm" onClick={cancel}>Hủy phiếu</button>
        </div>
      )}
    </Modal>
  )
}

function DocForm({ onClose, onSaved }) {
  const [type, setType] = useState(0)
  const [whs, setWhs] = useState([]); const [prods, setProds] = useState([])
  const [f, setF] = useState({ fromWarehouseId: '', toWarehouseId: '', partnerName: '', refNo: '', note: '' })
  const [lines, setLines] = useState([{ productId: '', quantity: 1, unitPrice: 0 }])
  const [err, setErr] = useState('')
  useEffect(() => { api.warehouses().then(r => setWhs(r.data)); api.products().then(r => setProds(r.data)) }, [])
  const up = (k, v) => setF({ ...f, [k]: v })
  const setLine = (i, k, v) => setLines(lines.map((l, j) => j === i ? { ...l, [k]: v } : l))
  const addLine = () => setLines([...lines, { productId: '', quantity: 1, unitPrice: 0 }])
  const delLine = (i) => setLines(lines.filter((_, j) => j !== i))
  const total = lines.reduce((s, l) => s + (Number(l.quantity) || 0) * (Number(l.unitPrice) || 0), 0)
  const save = async () => {
    try {
      const payload = {
        type: Number(type),
        fromWarehouseId: (type === 1 || type === 2) && f.fromWarehouseId ? Number(f.fromWarehouseId) : null,
        toWarehouseId: (type === 0 || type === 2) && f.toWarehouseId ? Number(f.toWarehouseId) : null,
        partnerName: f.partnerName, refNo: f.refNo, note: f.note,
        lines: lines.filter(l => l.productId && Number(l.quantity) !== 0).map(l => ({ productId: Number(l.productId), quantity: Number(l.quantity), unitPrice: Number(l.unitPrice) || 0 }))
      }
      if (payload.lines.length === 0) { setErr('Cần ít nhất 1 dòng hàng.'); return }
      await api.createDoc(payload)
      onSaved()
    } catch (e) { setErr(e.message) }
  }
  return (
    <Modal title="Tạo phiếu kho" onClose={onClose} wide>
      {err && <Flash msg={{ ok: false, text: err }} />}
      <div className="row">
        <Field label="Loại phiếu"><select value={type} onChange={e => setType(Number(e.target.value))}>{DOCTYPES.map((s, i) => <option key={i} value={i}>{s}</option>)}</select></Field>
        {(type === 1 || type === 2) && <Field label="Từ kho"><select value={f.fromWarehouseId} onChange={e => up('fromWarehouseId', e.target.value)}><option value="">—</option>{whs.map(w => <option key={w.id} value={w.id}>{w.name}</option>)}</select></Field>}
        {(type === 0 || type === 2) && <Field label="Đến kho"><select value={f.toWarehouseId} onChange={e => up('toWarehouseId', e.target.value)}><option value="">—</option>{whs.map(w => <option key={w.id} value={w.id}>{w.name}</option>)}</select></Field>}
      </div>
      <div className="row"><Field label="Đối tác (NCC/khách)"><input value={f.partnerName} onChange={e => up('partnerName', e.target.value)} /></Field>
        <Field label="Chứng từ gốc"><input value={f.refNo} onChange={e => up('refNo', e.target.value)} /></Field></div>
      <Field label="Ghi chú"><input value={f.note} onChange={e => up('note', e.target.value)} /></Field>
      <div className="section-t">Dòng hàng</div>
      <table>
        <thead><tr><th>Mặt hàng</th><th style={{ width: 90 }}>SL</th><th style={{ width: 140 }}>Đơn giá</th><th></th></tr></thead>
        <tbody>{lines.map((l, i) => (
          <tr key={i}>
            <td><select value={l.productId} onChange={e => setLine(i, 'productId', e.target.value)}><option value="">— chọn —</option>{prods.map(p => <option key={p.id} value={p.id}>{p.code} · {p.name}</option>)}</select></td>
            <td><input type="number" value={l.quantity} onChange={e => setLine(i, 'quantity', e.target.value)} /></td>
            <td><input type="number" value={l.unitPrice} onChange={e => setLine(i, 'unitPrice', e.target.value)} /></td>
            <td>{lines.length > 1 && <button className="btn gray sm" onClick={() => delLine(i)}>×</button>}</td>
          </tr>))}</tbody>
      </table>
      <div className="row" style={{ marginTop: 8 }}><button className="btn ghost sm" style={{ flex: 'none' }} onClick={addLine}>+ Thêm dòng</button>
        <div className="sp" /><div style={{ flex: 'none', fontWeight: 700 }}>Tổng: {fmtMoney(total)}</div></div>
      <div style={{ marginTop: 16 }}><button className="btn" onClick={save}>Tạo phiếu (Nháp)</button></div>
    </Modal>
  )
}

function Products() {
  const [rows, setRows] = useState([]); const [show, setShow] = useState(false)
  const load = () => api.products().then(r => setRows(r.data))
  useEffect(() => { load() }, [])
  return (
    <>
      <div className="toolbar"><h1 style={{ margin: 0, flex: 1 }}>Sản phẩm / Vật tư</h1>
        <button className="btn sm" style={{ flex: 'none' }} onClick={() => setShow(true)}>+ Thêm</button></div>
      <div className="card" style={{ padding: 0, overflow: 'auto' }}>
        <table>
          <thead><tr><th>Mã</th><th>Tên</th><th>Nhóm</th><th>ĐVT</th><th className="right">Giá vốn</th><th className="right">Giá bán</th><th className="right">Tồn min/max</th></tr></thead>
          <tbody>{rows.map(p => (
            <tr key={p.id}><td>{p.code}</td><td>{p.name}</td><td>{p.category || '—'}</td><td>{p.uom}</td>
              <td className="right">{fmtMoney(p.costPrice)}</td><td className="right">{fmtMoney(p.salePrice)}</td>
              <td className="right muted">{p.minStock}/{p.maxStock}</td></tr>))}</tbody>
        </table>
      </div>
      {show && <ProductForm onClose={() => setShow(false)} onSaved={() => { setShow(false); load() }} />}
    </>
  )
}

function ProductForm({ onClose, onSaved }) {
  const [f, setF] = useState({ name: '', code: '', uom: 'cái', category: '', costPrice: 0, salePrice: 0, minStock: 0, maxStock: 0 })
  const [err, setErr] = useState(''); const up = (k, v) => setF({ ...f, [k]: v })
  const save = async () => {
    try { await api.createProduct({ ...f, costPrice: Number(f.costPrice), salePrice: Number(f.salePrice), minStock: Number(f.minStock), maxStock: Number(f.maxStock) }); onSaved() }
    catch (e) { setErr(e.message) }
  }
  return (
    <Modal title="Thêm sản phẩm" onClose={onClose}>
      {err && <Flash msg={{ ok: false, text: err }} />}
      <div className="row"><Field label="Tên *"><input value={f.name} onChange={e => up('name', e.target.value)} /></Field>
        <Field label="Mã"><input value={f.code} onChange={e => up('code', e.target.value)} /></Field></div>
      <div className="row"><Field label="Nhóm"><input value={f.category} onChange={e => up('category', e.target.value)} /></Field>
        <Field label="ĐVT"><input value={f.uom} onChange={e => up('uom', e.target.value)} /></Field></div>
      <div className="row"><Field label="Giá vốn"><input type="number" value={f.costPrice} onChange={e => up('costPrice', e.target.value)} /></Field>
        <Field label="Giá bán"><input type="number" value={f.salePrice} onChange={e => up('salePrice', e.target.value)} /></Field></div>
      <div className="row"><Field label="Tồn min"><input type="number" value={f.minStock} onChange={e => up('minStock', e.target.value)} /></Field>
        <Field label="Tồn max"><input type="number" value={f.maxStock} onChange={e => up('maxStock', e.target.value)} /></Field></div>
      <div style={{ marginTop: 16 }}><button className="btn" onClick={save}>Lưu</button></div>
    </Modal>
  )
}

function Warehouses() {
  const [rows, setRows] = useState([]); const [show, setShow] = useState(false)
  const load = () => api.warehouses().then(r => setRows(r.data))
  useEffect(() => { load() }, [])
  return (
    <>
      <div className="toolbar"><h1 style={{ margin: 0, flex: 1 }}>Kho</h1>
        <button className="btn sm" style={{ flex: 'none' }} onClick={() => setShow(true)}>+ Thêm kho</button></div>
      <div className="card" style={{ padding: 0, overflow: 'auto' }}>
        <table>
          <thead><tr><th>Mã</th><th>Tên</th><th>Địa chỉ</th><th>Thủ kho</th></tr></thead>
          <tbody>{rows.map(w => (<tr key={w.id}><td>{w.code}</td><td>{w.name}</td><td>{w.address || '—'}</td><td>{w.keeper || '—'}</td></tr>))}</tbody>
        </table>
      </div>
      {show && <WarehouseForm onClose={() => setShow(false)} onSaved={() => { setShow(false); load() }} />}
    </>
  )
}

function WarehouseForm({ onClose, onSaved }) {
  const [f, setF] = useState({ name: '', code: '', address: '', keeper: '' })
  const [err, setErr] = useState(''); const up = (k, v) => setF({ ...f, [k]: v })
  const save = async () => { try { await api.createWarehouse(f); onSaved() } catch (e) { setErr(e.message) } }
  return (
    <Modal title="Thêm kho" onClose={onClose}>
      {err && <Flash msg={{ ok: false, text: err }} />}
      <div className="row"><Field label="Tên *"><input value={f.name} onChange={e => up('name', e.target.value)} /></Field>
        <Field label="Mã"><input value={f.code} onChange={e => up('code', e.target.value)} /></Field></div>
      <Field label="Địa chỉ"><input value={f.address} onChange={e => up('address', e.target.value)} /></Field>
      <Field label="Thủ kho"><input value={f.keeper} onChange={e => up('keeper', e.target.value)} /></Field>
      <div style={{ marginTop: 16 }}><button className="btn" onClick={save}>Lưu</button></div>
    </Modal>
  )
}

export default function App() {
  return (
    <Routes>
      <Route path="/" element={<Layout />}>
        <Route index element={<Dashboard />} />
        <Route path="balances" element={<Balances />} />
        <Route path="docs" element={<Docs />} />
        <Route path="products" element={<Products />} />
        <Route path="warehouses" element={<Warehouses />} />
      </Route>
    </Routes>
  )
}
