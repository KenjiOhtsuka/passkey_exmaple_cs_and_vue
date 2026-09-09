async function api(path, options = {}) {
  const headers = { ...(options.headers || {}) }
  if (options.body && !headers['Content-Type']) {
    headers['Content-Type'] = 'application/json'
  }
  const res = await fetch(`/api${path}`, {
    credentials: 'same-origin',
    ...options,
    headers
  })
  if (res.status === 204) {
    return null
  }
  const text = await res.text()
  const data = text ? JSON.parse(text) : null
  if (!res.ok) {
    throw new Error(data?.message || `Request failed (${res.status})`)
  }
  return data
}

export const authApi = {
  register: (email, password) =>
    api('/auth/register', { method: 'POST', body: JSON.stringify({ email, password }) }),

  login: (email, password) =>
    api('/auth/login', { method: 'POST', body: JSON.stringify({ email, password }) }),

  logout: () => api('/auth/logout', { method: 'POST' }),

  me: () => api('/auth/me'),

  passkeyCreateOptions: () =>
    api('/auth/passkey/create-options', { method: 'POST' }),

  passkeyRegister: (credentialJson, name) =>
    api('/auth/passkey/register', {
      method: 'POST',
      body: JSON.stringify({ credentialJson, name })
    }),

  passkeyLoginOptions: (username) =>
    api('/auth/passkey/login-options', { method: 'POST', body: JSON.stringify({ username }) }),

  passkeyLogin: (credentialJson) =>
    api('/auth/passkey/login', { method: 'POST', body: JSON.stringify({ credentialJson }) }),

  listPasskeys: () => api('/auth/passkeys'),

  deletePasskey: (id) => api(`/auth/passkeys/${encodeURIComponent(id)}`, { method: 'DELETE' })
}