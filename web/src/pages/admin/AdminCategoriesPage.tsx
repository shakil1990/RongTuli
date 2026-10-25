import { useQuery, useQueryClient } from '@tanstack/react-query'
import { useState, type FormEvent } from 'react'
import { useAdminCall } from '../../auth/useAdminCall'
import { Button } from '../../components/common/Button'
import { Skeleton } from '../../components/common/Skeleton'
import { alertBanner, cardSurface, fieldControl } from '../../components/common/styles'
import { ApiError } from '../../lib/api'
import {
  createCategory,
  deleteCategory,
  refreshProducts,
  updateCategory,
} from '../../services/admin'
import { catalogKeys, fetchCategories } from '../../services/catalog'
import type { CategoryInput } from '../../types/admin'
import type { Category } from '../../types/catalog'

const CHANGE_FAILED = 'That did not go through. Check your connection and try again.'

function messageOf(caught: unknown): string {
  return caught instanceof ApiError ? caught.message : CHANGE_FAILED
}

/** An empty order field means "leave it to the API" (0 for a new category, unchanged on edit). */
function readSortOrder(text: string): number | null {
  const value = Number(text.trim())
  return text.trim() === '' || !Number.isInteger(value) ? null : value
}

type CategoryFieldsProps = {
  idPrefix: string
  name: string
  sortOrder: string
  onNameChange: (value: string) => void
  onSortOrderChange: (value: string) => void
}

function CategoryFields({
  idPrefix,
  name,
  sortOrder,
  onNameChange,
  onSortOrderChange,
}: CategoryFieldsProps) {
  return (
    <>
      <div className="min-w-0 flex-1 space-y-1">
        <label htmlFor={`${idPrefix}-name`} className="block text-xs text-muted">
          Name
        </label>
        <input
          id={`${idPrefix}-name`}
          value={name}
          onChange={(event) => onNameChange(event.target.value)}
          maxLength={100}
          required
          autoComplete="off"
          className={fieldControl}
        />
      </div>
      <div className="w-24 space-y-1">
        <label htmlFor={`${idPrefix}-order`} className="block text-xs text-muted">
          Menu order
        </label>
        <input
          id={`${idPrefix}-order`}
          value={sortOrder}
          onChange={(event) => onSortOrderChange(event.target.value)}
          inputMode="numeric"
          autoComplete="off"
          className={fieldControl}
        />
      </div>
    </>
  )
}

type CategoryRowProps = {
  category: Category
  onSave: (input: CategoryInput) => Promise<void>
  onDelete: () => Promise<void>
}

function CategoryRow({ category, onSave, onDelete }: CategoryRowProps) {
  const [isEditing, setIsEditing] = useState(false)
  const [name, setName] = useState(category.name)
  const [sortOrder, setSortOrder] = useState(String(category.sortOrder))
  const [isBusy, setIsBusy] = useState(false)
  const [problem, setProblem] = useState<string | null>(null)

  const attempt = async (change: () => Promise<void>) => {
    setProblem(null)
    setIsBusy(true)
    try {
      await change()
      return true
    } catch (caught) {
      setProblem(messageOf(caught))
      return false
    } finally {
      setIsBusy(false)
    }
  }

  const handleSave = async (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault()
    const saved = await attempt(() => onSave({ name: name.trim(), sortOrder: readSortOrder(sortOrder) }))
    if (saved) setIsEditing(false)
  }

  const cancel = () => {
    setName(category.name)
    setSortOrder(String(category.sortOrder))
    setProblem(null)
    setIsEditing(false)
  }

  const handleDelete = () => {
    if (!window.confirm(`Delete the category “${category.name}”?`)) return
    void attempt(onDelete)
  }

  return (
    <li className="space-y-2 px-4 py-3">
      {isEditing ? (
        <form onSubmit={handleSave} className="flex flex-wrap items-end gap-3">
          <CategoryFields
            idPrefix={`category-${category.id}`}
            name={name}
            sortOrder={sortOrder}
            onNameChange={setName}
            onSortOrderChange={setSortOrder}
          />
          <div className="flex gap-2">
            <Button type="submit" size="sm" disabled={isBusy}>
              {isBusy ? 'Saving…' : 'Save'}
            </Button>
            <Button variant="ghost" size="sm" onClick={cancel} disabled={isBusy}>
              Cancel
            </Button>
          </div>
        </form>
      ) : (
        <div className="flex flex-wrap items-center justify-between gap-3">
          <div>
            <p className="font-medium">{category.name}</p>
            <p className="text-xs text-muted">
              /c/{category.slug} · menu order {category.sortOrder}
            </p>
          </div>
          <div className="flex gap-2">
            <Button variant="secondary" size="sm" onClick={() => setIsEditing(true)} disabled={isBusy}>
              Edit
            </Button>
            <Button variant="ghost" size="sm" onClick={handleDelete} disabled={isBusy}>
              {isBusy ? 'Deleting…' : 'Delete'}
            </Button>
          </div>
        </div>
      )}

      {problem && (
        <p role="alert" className={alertBanner}>
          {problem}
        </p>
      )}
    </li>
  )
}

export function AdminCategoriesPage() {
  const call = useAdminCall()
  const queryClient = useQueryClient()
  const [name, setName] = useState('')
  const [sortOrder, setSortOrder] = useState('')
  const [isAdding, setIsAdding] = useState(false)
  const [problem, setProblem] = useState<string | null>(null)

  const { data, isPending, isError } = useQuery({
    queryKey: catalogKeys.categories,
    queryFn: ({ signal }) => fetchCategories(signal),
  })

  // The storefront menu reads the same list, and product rows show a category's slug.
  const reload = async () => {
    await Promise.all([
      queryClient.invalidateQueries({ queryKey: catalogKeys.categories }),
      refreshProducts(queryClient),
    ])
  }

  const add = async (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault()
    setProblem(null)
    setIsAdding(true)
    try {
      await call((token) => createCategory({ name: name.trim(), sortOrder: readSortOrder(sortOrder) }, token))
      await reload()
      setName('')
      setSortOrder('')
    } catch (caught) {
      setProblem(messageOf(caught))
    } finally {
      setIsAdding(false)
    }
  }

  return (
    <div className="space-y-5">
      <form onSubmit={add} className={`${cardSurface} space-y-3 p-5`}>
        <h2 className="text-xl">Add a category</h2>
        <div className="flex flex-wrap items-end gap-3">
          <CategoryFields
            idPrefix="new-category"
            name={name}
            sortOrder={sortOrder}
            onNameChange={setName}
            onSortOrderChange={setSortOrder}
          />
          <Button type="submit" disabled={isAdding || name.trim() === ''}>
            {isAdding ? 'Adding…' : 'Add category'}
          </Button>
        </div>
        <p className="text-xs text-muted">
          Lower menu order comes first in the shop menu. A category with products in it cannot be
          deleted.
        </p>
        {problem && (
          <p role="alert" className={alertBanner}>
            {problem}
          </p>
        )}
      </form>

      {isError && (
        <p role="alert" className={alertBanner}>
          The categories could not be loaded. Check your connection and reload the page.
        </p>
      )}

      {isPending && !isError && <Skeleton className="h-48 w-full" />}

      {data && data.length === 0 && (
        <p className="text-sm text-muted">No categories yet. Add one above.</p>
      )}

      {data && data.length > 0 && (
        <ul className={`${cardSurface} divide-y divide-line`} aria-label="Categories">
          {data.map((category) => (
            <CategoryRow
              key={category.id}
              category={category}
              onSave={async (input) => {
                await call((token) => updateCategory(category.id, input, token))
                await reload()
              }}
              onDelete={async () => {
                await call((token) => deleteCategory(category.id, token))
                await reload()
              }}
            />
          ))}
        </ul>
      )}
    </div>
  )
}
