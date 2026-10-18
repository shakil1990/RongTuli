import { useQuery } from '@tanstack/react-query'
import { Link, useParams } from 'react-router-dom'
import { CatalogBrowser } from '../components/CatalogBrowser'
import { EmptyState } from '../components/common/EmptyState'
import { buttonClasses } from '../components/common/styles'
import { catalogKeys, fetchCategories } from '../services/catalog'

export function CategoryPage() {
  const { slug = '' } = useParams()

  const { data: categories } = useQuery({
    queryKey: catalogKeys.categories,
    queryFn: ({ signal }) => fetchCategories(signal),
    staleTime: 5 * 60 * 1000,
  })

  const category = categories?.find((candidate) => candidate.slug === slug)

  return (
    <div className="space-y-6">
      <div className="space-y-2">
        <nav aria-label="Breadcrumb" className="text-sm text-muted">
          <Link to="/" className="hover:text-accent hover:underline">
            Shop
          </Link>
          <span aria-hidden="true"> / </span>
          <span className="text-ink">{category?.name ?? slug}</span>
        </nav>
        <h1 className="text-3xl">{category?.name ?? slug}</h1>
      </div>

      <CatalogBrowser
        category={slug}
        empty={
          <EmptyState
            title="Nothing here just yet"
            description={
              category
                ? `There are no products in ${category.name} at the moment.`
                : 'This category does not exist, or it has no products.'
            }
            action={
              <Link to="/" className={buttonClasses('secondary', 'md', 'mt-2')}>
                Browse everything
              </Link>
            }
          />
        }
      />
    </div>
  )
}
