import { httpClient } from '@/api/httpClient';
import type { PaginatedResponse } from '@/api/types/api.types';
import { resolveAssetUrl } from '@/api/utils/assetUrl';
import type { MarketplaceListing, MarketplaceListingCondition } from '@/types/marketplace.types';
import { toPriceDisplayValue } from '@/utils/priceDisplay';

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
  supportsOnlinePayment: unknown;
  supportsCashOnDelivery: unknown;
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

export interface UpsertListingRequest {
  bookId: string;
  price: number;
  currency: string;
  condition: MarketplaceListingCondition;
  quantity: number;
  description: string;
  image?: File | null;
  removeImage?: boolean;
}

export interface CreateListingWithBookRequest {
  title: string;
  author: string;
  genre: string;
  bookDescription?: string;
  publisher?: string;
  publishedOn?: string;
  isbn?: string;
  price: number;
  currency: string;
  condition: MarketplaceListingCondition;
  quantity: number;
  description: string;
  image?: File | null;
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

const toBackendListingCondition = (condition: MarketplaceListingCondition): number => {
  if (condition === 'new') {
    return 0;
  }

  if (condition === 'likeNew') {
    return 1;
  }

  if (condition === 'veryGood') {
    return 2;
  }

  if (condition === 'good') {
    return 3;
  }

  if (condition === 'poor') {
    return 5;
  }

  return 4;
};

const toSellerPaymentSupportFlag = (value: unknown, fieldName: string): boolean => {
  if (typeof value !== 'boolean') {
    throw new Error(`Invalid listing payment support field: ${fieldName}`);
  }

  return value;
};

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
  supportsOnlinePayment: toSellerPaymentSupportFlag(
    listing.supportsOnlinePayment,
    'supportsOnlinePayment',
  ),
  supportsCashOnDelivery: toSellerPaymentSupportFlag(
    listing.supportsCashOnDelivery,
    'supportsCashOnDelivery',
  ),
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

const createListingFormData = (payload: UpsertListingRequest): FormData => {
  const formData = new FormData();
  formData.append('bookId', payload.bookId);
  formData.append('price', payload.price.toString());
  formData.append('currency', payload.currency.trim().toUpperCase());
  formData.append('condition', toBackendListingCondition(payload.condition).toString());
  formData.append('quantity', payload.quantity.toString());
  formData.append('description', payload.description.trim());
  formData.append('removeImage', String(Boolean(payload.removeImage)));

  if (payload.image) {
    formData.append('image', payload.image);
  }

  return formData;
};

const createListingWithBookFormData = (payload: CreateListingWithBookRequest): FormData => {
  const formData = new FormData();
  formData.append('title', payload.title.trim());
  formData.append('author', payload.author.trim());
  formData.append('genre', payload.genre.trim());

  const trimmedBookDescription = payload.bookDescription?.trim();
  if (trimmedBookDescription) {
    formData.append('bookDescription', trimmedBookDescription);
  }

  const trimmedPublisher = payload.publisher?.trim();
  if (trimmedPublisher) {
    formData.append('publisher', trimmedPublisher);
  }

  if (payload.publishedOn) {
    formData.append('publishedOn', payload.publishedOn);
  }

  const trimmedIsbn = payload.isbn?.trim();
  if (trimmedIsbn) {
    formData.append('isbn', trimmedIsbn);
  }

  formData.append('price', payload.price.toString());
  formData.append('currency', payload.currency.trim().toUpperCase());
  formData.append('condition', toBackendListingCondition(payload.condition).toString());
  formData.append('quantity', payload.quantity.toString());
  formData.append('description', payload.description.trim());

  if (payload.image) {
    formData.append('image', payload.image);
  }

  return formData;
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

    const response = await httpClient.get<PaginatedResponse<ListingApiModel>>(
      `${LISTINGS_BASE_PATH}/mine/`,
      {
        params: removeEmptyQueryValues(backendQuery),
      },
    );

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

  async createListing(payload: UpsertListingRequest): Promise<string> {
    const response = await httpClient.post<string>(
      LISTINGS_BASE_PATH,
      createListingFormData(payload),
      {
        headers: { 'Content-Type': 'multipart/form-data' },
      },
    );

    return response.data;
  },

  async createListingWithBook(payload: CreateListingWithBookRequest): Promise<string> {
    const response = await httpClient.post<string>(
      `${LISTINGS_BASE_PATH}/with-book/`,
      createListingWithBookFormData(payload),
      {
        headers: { 'Content-Type': 'multipart/form-data' },
      },
    );

    return response.data;
  },

  async editListing(id: string, payload: UpsertListingRequest): Promise<void> {
    await httpClient.put(`${LISTINGS_BASE_PATH}/${id}/`, createListingFormData(payload), {
      headers: { 'Content-Type': 'multipart/form-data' },
    });
  },
};
