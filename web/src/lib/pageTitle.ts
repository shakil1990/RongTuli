import { useEffect } from 'react'

export const SITE_NAME = 'RongTuli'

/** The home page's title, and what a page shows while the thing it names is still loading. */
export const SITE_TITLE = `${SITE_NAME} — everyday clothing, cash on delivery`

/**
 * Puts the page first and the shop last, so a row of browser tabs or a history list still
 * tells pages apart when the titles are cut short.
 */
export function formatPageTitle(name: string | null): string {
  const trimmed = name?.trim()
  return trimmed ? `${trimmed} · ${SITE_NAME}` : SITE_TITLE
}

/**
 * Sets the browser tab's title for the current page. Pass null for the home page, or while
 * the product or category the title names is still loading.
 */
export function usePageTitle(name: string | null) {
  useEffect(() => {
    document.title = formatPageTitle(name)
  }, [name])
}
