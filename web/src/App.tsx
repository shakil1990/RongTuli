import { GoogleOAuthProvider } from '@react-oauth/google'
import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import type { ReactNode } from 'react'
import { BrowserRouter, Navigate, Route, Routes } from 'react-router-dom'
import { AuthProvider } from './auth/AuthProvider'
import { googleClientId } from './auth/googleClient'
import { CartProvider } from './cart/CartProvider'
import { Layout } from './components/Layout'
import { RequireAdmin } from './components/RequireAdmin'
import { AdminLayout } from './components/admin/AdminLayout'
import { CartPage } from './pages/CartPage'
import { CategoryPage } from './pages/CategoryPage'
import { CheckoutPage } from './pages/CheckoutPage'
import { HomePage } from './pages/HomePage'
import { OrderPage } from './pages/OrderPage'
import { OrdersPage } from './pages/OrdersPage'
import { ProductPage } from './pages/ProductPage'
import { AdminCategoriesPage } from './pages/admin/AdminCategoriesPage'
import { AdminOrderPage } from './pages/admin/AdminOrderPage'
import { AdminOrdersPage } from './pages/admin/AdminOrdersPage'
import { VerifiedPhoneProvider } from './phone/VerifiedPhoneProvider'
import { AdminProductEditorPage } from './pages/admin/AdminProductEditorPage'
import { AdminProductsPage } from './pages/admin/AdminProductsPage'

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
            <VerifiedPhoneProvider>
              <BrowserRouter>
                <Routes>
                  <Route element={<Layout />}>
                    <Route path="/" element={<HomePage />} />
                    <Route path="/c/:slug" element={<CategoryPage />} />
                    <Route path="/p/:slug" element={<ProductPage />} />
                    <Route path="/cart" element={<CartPage />} />
                    <Route path="/checkout" element={<CheckoutPage />} />
                    <Route path="/orders" element={<OrdersPage />} />
                    <Route path="/orders/:id" element={<OrderPage />} />
                    <Route
                      path="/admin"
                      element={
                        <RequireAdmin>
                          <AdminLayout />
                        </RequireAdmin>
                      }
                    >
                      <Route index element={<Navigate to="products" replace />} />
                      <Route path="products" element={<AdminProductsPage />} />
                      <Route path="products/new" element={<AdminProductEditorPage />} />
                      <Route path="products/:id" element={<AdminProductEditorPage />} />
                      <Route path="categories" element={<AdminCategoriesPage />} />
                      <Route path="orders" element={<AdminOrdersPage />} />
                      <Route path="orders/:id" element={<AdminOrderPage />} />
                      <Route path="*" element={<Navigate to="products" replace />} />
                    </Route>
                  </Route>
                </Routes>
              </BrowserRouter>
            </VerifiedPhoneProvider>
          </CartProvider>
        </AuthProvider>
      </GoogleIdentity>
    </QueryClientProvider>
  )
}
