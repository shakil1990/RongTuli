import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { BrowserRouter, Route, Routes } from 'react-router-dom'
import { Layout } from './components/Layout'
import { Placeholder } from './pages/Placeholder'

const queryClient = new QueryClient()

export default function App() {
  return (
    <QueryClientProvider client={queryClient}>
      <BrowserRouter>
        <Routes>
          <Route element={<Layout />}>
            <Route path="/" element={<Placeholder title="Home" />} />
            <Route path="/c/:slug" element={<Placeholder title="Category" />} />
            <Route path="/p/:slug" element={<Placeholder title="Product" />} />
            <Route path="/cart" element={<Placeholder title="Cart" />} />
            <Route path="/checkout" element={<Placeholder title="Checkout" />} />
            <Route path="/orders" element={<Placeholder title="My orders" />} />
            <Route path="/admin/*" element={<Placeholder title="Admin" />} />
          </Route>
        </Routes>
      </BrowserRouter>
    </QueryClientProvider>
  )
}
