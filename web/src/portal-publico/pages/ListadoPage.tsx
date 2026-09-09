import { useState } from 'react'
import { useOutletContext, useSearchParams } from 'react-router'
import { usePublicListings } from '@/features/public/hooks/usePublic'
import { parseListingFilters, serializeListingFilters } from '@/features/public/utils/filters'
import type { PublicListingFilters, PublicSortOption } from '@/features/public/types/public.types'
import type { PublicoOutletContext } from '../types'
import { ListingCard, ListingCardSkeleton } from '../components/ListingCard'
import { FilterRail } from '../components/FilterRail'
import { SortBar } from '../components/SortBar'
import { FilterChips } from '../components/FilterChips'
import { Pagination } from '../components/Pagination'
import { EmptySearchIcon } from '../components/icons'

export default function ListadoPage() {
  const { slug } = useOutletContext<PublicoOutletContext>()
  const [searchParams, setSearchParams] = useSearchParams()
  const [filtersOpen, setFiltersOpen] = useState(false)

  const filters = parseListingFilters(searchParams)
  const { data, isLoading, isFetching, isError } = usePublicListings(slug, filters)

  function patch(next: Partial<PublicListingFilters>) {
    setSearchParams(serializeListingFilters({ ...filters, ...next }))
  }

  function clearAll() {
    setSearchParams(new URLSearchParams())
  }

  return (
    <div className="wrap listado">
      <FilterRail
        facets={data?.facets}
        filters={filters}
        onPatch={patch}
        onClear={clearAll}
        open={filtersOpen}
        onClose={() => setFiltersOpen(false)}
      />

      <section>
        <SortBar
          total={data?.total ?? 0}
          sort={filters.sort}
          onSortChange={(sort: PublicSortOption | undefined) => patch({ sort })}
          onToggleFilters={() => setFiltersOpen((o) => !o)}
        />
        <FilterChips filters={filters} onPatch={patch} />

        {isError ? (
          <div className="state-box">No pudimos cargar las propiedades. Probá de nuevo en unos minutos.</div>
        ) : (
          <>
            <div className="grid" aria-busy={isFetching}>
              {isLoading
                ? Array.from({ length: 6 }).map((_, i) => <ListingCardSkeleton key={i} />)
                : data && data.items.length > 0
                  ? data.items.map((listing) => <ListingCard key={listing.id} slug={slug} listing={listing} />)
                  : (
                    <div className="empty">
                      <EmptySearchIcon />
                      <div>No hay propiedades con esos filtros.</div>
                    </div>
                  )}
            </div>
            {data ? (
              <Pagination
                page={data.page}
                pageSize={data.pageSize}
                total={data.total}
                onPageChange={(page) => patch({ page })}
              />
            ) : null}
          </>
        )}
      </section>
    </div>
  )
}
