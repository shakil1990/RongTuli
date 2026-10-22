import { useState, type FormEvent } from 'react'
import { ApiError } from '../../lib/api'
import {
  formErrorsFromApi,
  parseProductForm,
  type ProductFormErrors,
  type ProductFormField,
  type ProductFormInput,
  type ProductFormValues,
} from '../../lib/productForm'
import type { Category } from '../../types/catalog'
import { Button } from '../common/Button'
import { Field } from '../common/Field'
import { describedBy } from '../common/describedBy'
import { alertBanner, cardSurface, fieldControl } from '../common/styles'

const SAVE_FAILED = 'The product could not be saved. Check your connection and try again.'

type ProductFormProps = {
  initialValues: ProductFormValues
  categories: Category[]
  submitLabel: string
  /** Rejects with the API's error so the form can put each message beside its field. */
  onSave: (input: ProductFormInput) => Promise<void>
}

export function ProductForm({ initialValues, categories, submitLabel, onSave }: ProductFormProps) {
  const [values, setValues] = useState(initialValues)
  const [errors, setErrors] = useState<ProductFormErrors>({})
  const [problem, setProblem] = useState<string | null>(null)
  const [isSaving, setIsSaving] = useState(false)

  const update = (field: ProductFormField) => (value: string) => {
    setValues((current) => ({ ...current, [field]: value }))
    setErrors((current) => ({ ...current, [field]: undefined }))
  }

  const handleSubmit = async (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault()
    setProblem(null)

    const result = parseProductForm(values)
    if (!result.ok) {
      setErrors(result.errors)
      return
    }

    setIsSaving(true)
    try {
      await onSave(result.input)
    } catch (caught) {
      const fieldErrors = caught instanceof ApiError ? formErrorsFromApi(caught.fieldErrors) : {}
      setErrors(fieldErrors)
      // A message already shown beside its field need not be repeated above the form.
      if (Object.keys(fieldErrors).length === 0) {
        setProblem(caught instanceof ApiError ? caught.message : SAVE_FAILED)
      }
    } finally {
      setIsSaving(false)
    }
  }

  return (
    <form onSubmit={handleSubmit} noValidate className={`${cardSurface} space-y-5 p-5 sm:p-6`}>
      {problem && (
        <p role="alert" className={alertBanner}>
          {problem}
        </p>
      )}

      <Field id="product-name" label="Name" error={errors.name}>
        <input
          id="product-name"
          name="name"
          value={values.name}
          onChange={(event) => update('name')(event.target.value)}
          maxLength={200}
          autoComplete="off"
          className={fieldControl}
          {...describedBy('product-name', errors.name, false)}
        />
      </Field>

      <div className="grid gap-5 sm:grid-cols-2">
        <Field
          id="product-category"
          label="Category"
          hint={categories.length === 0 ? 'Add a category first, on the Categories tab.' : undefined}
          error={errors.categoryId}
        >
          <select
            id="product-category"
            name="categoryId"
            value={values.categoryId}
            onChange={(event) => update('categoryId')(event.target.value)}
            className={fieldControl}
            {...describedBy('product-category', errors.categoryId, categories.length === 0)}
          >
            <option value="">Choose a category</option>
            {categories.map((category) => (
              <option key={category.id} value={category.id}>
                {category.name}
              </option>
            ))}
          </select>
        </Field>

        <Field id="product-price" label="Price (৳)" error={errors.price}>
          <input
            id="product-price"
            name="price"
            inputMode="decimal"
            value={values.price}
            onChange={(event) => update('price')(event.target.value)}
            placeholder="1250"
            autoComplete="off"
            className={fieldControl}
            {...describedBy('product-price', errors.price, false)}
          />
        </Field>
      </div>

      <Field
        id="product-sizes"
        label="Sizes"
        hint="Separate with commas, e.g. S, M, L. Leave empty for one-size pieces."
        error={errors.sizes}
      >
        <input
          id="product-sizes"
          name="sizes"
          value={values.sizes}
          onChange={(event) => update('sizes')(event.target.value)}
          placeholder="S, M, L, XL"
          autoComplete="off"
          className={fieldControl}
          {...describedBy('product-sizes', errors.sizes, true)}
        />
      </Field>

      <Field id="product-description" label="Description" error={errors.description}>
        <textarea
          id="product-description"
          name="description"
          value={values.description}
          onChange={(event) => update('description')(event.target.value)}
          rows={5}
          maxLength={4000}
          className={fieldControl}
          {...describedBy('product-description', errors.description, false)}
        />
      </Field>

      <div className="flex justify-end">
        <Button type="submit" disabled={isSaving}>
          {isSaving ? 'Saving…' : submitLabel}
        </Button>
      </div>
    </form>
  )
}
