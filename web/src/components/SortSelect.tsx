import { PRODUCT_SORTS, SORT_LABELS, type ProductSort } from '../types/catalog'
import { focusRing } from './common/styles'

type SortSelectProps = {
  value: ProductSort
  onChange: (sort: ProductSort) => void
}

export function SortSelect({ value, onChange }: SortSelectProps) {
  return (
    <div className="flex items-center gap-2">
      <label htmlFor="select-sort" className="text-sm text-muted">
        Sort
      </label>
      <select
        id="select-sort"
        name="select-sort"
        value={value}
        onChange={(event) => onChange(event.target.value as ProductSort)}
        className={`${focusRing} rounded-full border border-line bg-raised py-2 pr-8 pl-3 text-sm
          text-ink transition hover:border-accent`}
      >
        {PRODUCT_SORTS.map((sort) => (
          <option key={sort} value={sort}>
            {SORT_LABELS[sort]}
          </option>
        ))}
      </select>
    </div>
  )
}
