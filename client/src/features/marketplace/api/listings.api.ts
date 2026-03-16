import { httpClient } from '@/api/httpClient';
import type { PaginatedResponse } from '@/api/types/api.types';
import { resolveAssetUrl } from '@/api/utils/assetUrl';
import type { MarketplaceListing, MarketplaceListingCondition } from '@/types/marketplace.types';
import type { CurrencyCode, PriceDisplayValue } from '@/types/pricing.types';

export interface ListingApiModel {
  id: string;
  bookId: string;
  bookTitle: string;
  bookAuthor: string;
  bookGenre: string;
  bookPublisher?: string | null;
  bookPublishedOn?: string | null;
  bookIsbn?: string | null;
  creatorId: string;
  price: number;
  currency: string;
  condition: number | string;
  quantity: number;
  description: string;
  imagePath: string;
  isApproved: boolean;
  rejectionReason?: string | null;
  createdOn: string;
  modifiedOn?: string | null;
  approvedOn?: string | null;
  approvedBy?: string | null;
}

export interface ListingFilterQuery {
  searchTerm?: string | undefined;
  title?: string | undefined;
  author?: string | undefined;
  genre?: string | undefined;
  condition?: number | undefined;
  priceFrom?: number | undefined;
  priceTo?: number | undefined;
  sorting?: number | undefined;
  pageIndex?: number | undefined;
  pageSize?: number | undefined;
  isApproved?: boolean | undefined;
}

interface ListingBackendFilterQuery {
  SearchTerm?: string | undefined;
  Title?: string | undefined;
  Author?: string | undefined;
  Genre?: string | undefined;
  Condition?: number | undefined;
  PriceFrom?: number | undefined;
  PriceTo?: number | undefined;
  Sorting?: number | undefined;
  PageIndex?: number | undefined;
  PageSize?: number | undefined;
  IsApproved?: boolean | undefined;
}

const LISTINGS_BASE_PATH = '/BookListings';

const toCurrencyCode = (currency: string): CurrencyCode => {
  const normalizedCurrency = currency.trim().toUpperCase();
  if (normalizedCurrency === 'EUR') {
    return 'EUR';
  }

  return 'BGN';
};

const toListingCondition = (condition: number | string): MarketplaceListingCondition => {
  if (typeof condition === 'string') {
    const normalizedCondition = condition.toLowerCase();
    if (
      normalizedCondition === 'new' ||
      normalizedCondition === 'likenew' ||
      normalizedCondition === 'verygood' ||
      normalizedCondition === 'good' ||
      normalizedCondition === 'acceptable' ||
      normalizedCondition === 'poor'
    ) {
      return normalizedCondition === 'likenew'
        ? 'likeNew'
        : normalizedCondition === 'verygood'
          ? 'veryGood'
          : (normalizedCondition as MarketplaceListingCondition);
    }
  }

  if (condition === 0) {
    return 'new';
  }

  if (condition === 1) {
    return 'likeNew';
  }

  if (condition === 2) {
    return 'veryGood';
  }

  if (condition === 3) {
    return 'good';
  }

  if (condition === 5) {
    return 'poor';
  }

  return 'acceptable';
};

const toPriceDisplayValue = (price: number, currency: string): PriceDisplayValue => ({
  primary: {
    amount: price,
    currency: toCurrencyCode(currency),
  },
});

const mapListing = (listing: ListingApiModel): MarketplaceListing => ({
  id: listing.id,
  bookId: listing.bookId,
  title: listing.bookTitle,
  author: listing.bookAuthor,
  genre: listing.bookGenre,
  publisher: listing.bookPublisher ?? null,
  publishedOn: listing.bookPublishedOn ?? null,
  isbn: listing.bookIsbn ?? null,
  creatorId: listing.creatorId,
  condition: toListingCondition(listing.condition),
  quantity: listing.quantity,
  description: listing.description,
  imageUrl: resolveAssetUrl(listing.imagePath),
  isApproved: listing.isApproved,
  rejectionReason: listing.rejectionReason ?? null,
  createdOn: listing.createdOn,
  modifiedOn: listing.modifiedOn ?? null,
  price: toPriceDisplayValue(listing.price, listing.currency),
});

const removeEmptyQueryValues = <T extends object>(query: T): T => {
  const entries = Object.entries(query as Record<string, unknown>).filter(
    ([, value]) => value !== undefined && value !== null && value !== '',
  );

  return Object.fromEntries(entries) as T;
};

export const listingsApi = {
  async getListings(query: ListingFilterQuery): Promise<PaginatedResponse<MarketplaceListing>> {
    const backendQuery: ListingBackendFilterQuery = {
      SearchTerm: query.searchTerm,
      Title: query.title,
      Author: query.author,
      Genre: query.genre,
      Condition: query.condition,
      PriceFrom: query.priceFrom,
      PriceTo: query.priceTo,
      Sorting: query.sorting,
      PageIndex: query.pageIndex,
      PageSize: query.pageSize,
      IsApproved: query.isApproved,
    };

    const response = await httpClient.get<PaginatedResponse<ListingApiModel>>(LISTINGS_BASE_PATH, {
      params: removeEmptyQueryValues(backendQuery),
    });

    return {
      ...response.data,
      items: response.data.items.map(mapListing),
    };
  },
  async getMineListings(query: ListingFilterQuery): Promise<PaginatedResponse<MarketplaceListing>> {
    const backendQuery: ListingBackendFilterQuery = {
      SearchTerm: query.searchTerm,
      Title: query.title,
      Author: query.author,
      Genre: query.genre,
      Condition: query.condition,
      PriceFrom: query.priceFrom,
      PriceTo: query.priceTo,
      Sorting: query.sorting,
      PageIndex: query.pageIndex,
      PageSize: query.pageSize,
      IsApproved: query.isApproved,
    };

    const response = await httpClient.get<PaginatedResponse<ListingApiModel>>(`${LISTINGS_BASE_PATH}/mine/`, {
      params: removeEmptyQueryValues(backendQuery),
    });

    return {
      ...response.data,
      items: response.data.items.map(mapListing),
    };
  },

  async getListingById(id: string): Promise<MarketplaceListing> {
    const response = await httpClient.get<ListingApiModel>(`${LISTINGS_BASE_PATH}/${id}/`);

    return mapListing(response.data);
  },

  async deleteListing(id: string): Promise<void> {
    await httpClient.delete(`${LISTINGS_BASE_PATH}/${id}/`);
  },
};
