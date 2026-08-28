// Lớp gọi API JSON tới backend ASP.NET (cùng origin, kèm cookie org_key multi-tenant).
const base = '/api/v1'

async function req(path, opts = {}) {
  const res = await fetch(base + path, {
    headers: { 'Content-Type': 'application/json' },
    credentials: 'same-origin',
    ...opts,
    body: opts.body ? JSON.stringify(opts.body) : undefined
  })
  const text = await res.text()
  const data = text ? JSON.parse(text) : null
  if (!res.ok) throw new Error(data?.error || `Lỗi ${res.status}`)
  return { data, cache: res.headers.get('X-Cache') }
}

export const api = {
  dashboard: () => req('/dashboard'),
  warehouses: () => req('/warehouses'),
  createWarehouse: (b) => req('/warehouses', { method: 'POST', body: b }),
  products: () => req('/products'),
  createProduct: (b) => req('/products', { method: 'POST', body: b }),
  docs: (type, status) => req(`/docs?${type != null ? `type=${type}&` : ''}${status != null ? `status=${status}` : ''}`),
  doc: (id) => req(`/docs/${id}`),
  createDoc: (b) => req('/docs', { method: 'POST', body: b }),
  postDoc: (id) => req(`/docs/${id}/post`, { method: 'POST' }),
  cancelDoc: (id) => req(`/docs/${id}/cancel`, { method: 'POST' }),
  balances: (warehouseId) => req(`/balances${warehouseId ? `?warehouseId=${warehouseId}` : ''}`)
}

export const fmtMoney = (n) => (n ?? 0).toLocaleString('vi-VN') + ' ₫'
export const fmtNum = (n) => (n ?? 0).toLocaleString('vi-VN')
export const fmtDate = (s) => s ? new Date(s).toLocaleDateString('vi-VN') : '—'

export const DOCTYPES = ['Nhập kho', 'Xuất kho', 'Chuyển kho']
export const DOCSTATUS = ['Nháp', 'Đã ghi sổ', 'Đã hủy']
