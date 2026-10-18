import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { BrowserRouter, Route, Routes } from 'react-router-dom'
import { CartProvider } from './cart/CartProvider'
import { Layout } from './components/Layout'
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

export default function App() {
  return (
    <QueryClientProvider client={queryClient}>
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
              <Route path="/admin/*" element={<Placeholder title="Admin" />} />
            </Route>
          </Routes>
        </BrowserRouter>
      </CartProvider>
    </QueryClientProvider>
  )
}
