import { createContext, useContext, useMemo, useState } from 'react'
import { apiGet, apiPost } from '../api/client'

const AuthContext = createContext(null)

function readStoredUser() {
  try {
    return JSON.parse(localStorage.getItem('leaveease_user'))
  } catch {
    return null
  }
}

export function AuthProvider({ children }) {
  const [user, setUser] = useState(readStoredUser)
  const [loading, setLoading] = useState(false)

  const login = async (email, password) => {
    setLoading(true)
    try {
      const result = await apiPost('/auth/login', { email, password })
      localStorage.setItem('leaveease_token', result.token)
      localStorage.setItem('leaveease_user', JSON.stringify(result.user))
      setUser(result.user)
      return result.user
    } finally {
      setLoading(false)
    }
  }

  const refreshUser = async () => {
    const result = await apiGet('/auth/me')
    localStorage.setItem('leaveease_user', JSON.stringify(result))
    setUser(result)
    return result
  }

  const logout = () => {
    localStorage.removeItem('leaveease_token')
    localStorage.removeItem('leaveease_user')
    setUser(null)
  }

  const hasRole = (role) => user?.roles?.includes(role) ?? false

  const value = useMemo(
    () => ({ user, loading, login, logout, refreshUser, hasRole }),
    [user, loading],
  )

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>
}

export function useAuth() {
  const value = useContext(AuthContext)
  if (!value) throw new Error('useAuth must be used inside AuthProvider')
  return value
}
