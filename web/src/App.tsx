import { GoogleOAuthProvider } from '@react-oauth/google'
import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import type { ReactNode } from 'react'
import { BrowserRouter, Route, Routes } from 'react-router-dom'
import { AuthProvider } from './auth/AuthProvider'
import { googleClientId } from './auth/googleClient'
import { CartProvider } from './cart/CartProvider'
import { Layout } from './components/Layout'
import { RequireAdmin } from './components/RequireAdmin'
import { CartPage } from './pages/CartPage'
import { CategoryPage } from './pages/CategoryPage'
import { HomePage } from './pages/HomePage'
import { Placeholder } from './pages/Placeholder'
import { ProductPage } from './pages/ProductPage'

const queryClient = new QueryClient({
  defaultOptions: {
    queries: {
      // The free API host can cold-start, so a catalog response is worth holding on to.
      staleTime: 60 * 1000,
      refetchOnWindowFocus: false,
    },
  },
})

/**
 * Google's script is only loaded where there is a client to load it for. A deployment without
 * one still serves the whole storefront; only the admin sign-in card says it is unconfigured.
 */
function GoogleIdentity({ children }: { children: ReactNode }) {
  if (!googleClientId) return <>{children}</>
  return <GoogleOAuthProvider clientId={googleClientId}>{children}</GoogleOAuthProvider>
}

export default function App() {
  return (
    <QueryClientProvider client={queryClient}>
      <GoogleIdentity>
        <AuthProvider>
          <CartProvider>
            <BrowserRouter>
              <Routes>
                <Route element={<Layout />}>
                  <Route path="/" element={<HomePage />} />
                  <Route path="/c/:slug" element={<CategoryPage />} />
                  <Route path="/p/:slug" element={<ProductPage />} />
                  <Route path="/cart" element={<CartPage />} />
                  <Route path="/checkout" element={<Placeholder title="Checkout" />} />
                  <Route path="/orders" element={<Placeholder title="My orders" />} />
                  <Route
                    path="/admin/*"
                    element={
                      <RequireAdmin>
                        <Placeholder title="Admin" />
                      </RequireAdmin>
                    }
                  />
                </Route>
              </Routes>
            </BrowserRouter>
          </CartProvider>
        </AuthProvider>
      </GoogleIdentity>
    </QueryClientProvider>
  )
}
