import { httpClient } from '@/api/httpClient';
import type { PaginatedResponse } from '@/api/types/api.types';

export interface BookModel {
  id: string;
  title: string;
  author: string;
  genre: string;
  description?: string | null;
  publisher?: string | null;
  publishedOn?: string | null;
  isbn?: string | null;
  creatorId: string;
  isApproved: boolean;
  rejectionReason?: string | null;
  createdOn: string;
  modifiedOn?: string | null;
  approvedOn?: string | null;
  approvedBy?: string | null;
}

export interface BookFilterQuery {
  searchTerm?: string | undefined;
  title?: string | undefined;
  author?: string | undefined;
  genre?: string | undefined;
  sorting?: number | undefined;
  pageIndex?: number | undefined;
  pageSize?: number | undefined;
  isApproved?: boolean | undefined;
}

interface BookBackendFilterQuery {
  SearchTerm?: string | undefined;
  Title?: string | undefined;
  Author?: string | undefined;
  Genre?: string | undefined;
  Sorting?: number | undefined;
  PageIndex?: number | undefined;
  PageSize?: number | undefined;
  IsApproved?: boolean | undefined;
}

const BOOKS_BASE_PATH = '/Books';

export interface BookLookupItem {
  id: string;
  title: string;
  author: string;
  genre: string;
  isbn?: string | null;
}

export interface CreateBookRequest {
  title: string;
  author: string;
  genre: string;
  description?: string;
  publisher?: string;
  publishedOn?: string;
  isbn?: string;
}

const removeEmptyQueryValues = <T extends object>(query: T): T => {
  const entries = Object.entries(query as Record<string, unknown>).filter(
    ([, value]) => value !== undefined && value !== null && value !== '',
  );

  return Object.fromEntries(entries) as T;
};

export const booksApi = {
  async getBooks(query: BookFilterQuery): Promise<PaginatedResponse<BookModel>> {
    const backendQuery: BookBackendFilterQuery = {
      SearchTerm: query.searchTerm,
      Title: query.title,
      Author: query.author,
      Genre: query.genre,
      Sorting: query.sorting,
      PageIndex: query.pageIndex,
      PageSize: query.pageSize,
      IsApproved: query.isApproved,
    };

    const response = await httpClient.get<PaginatedResponse<BookModel>>(BOOKS_BASE_PATH, {
      params: removeEmptyQueryValues(backendQuery),
    });

    return response.data;
  },

  async getBookById(id: string): Promise<BookModel> {
    const response = await httpClient.get<BookModel>(`${BOOKS_BASE_PATH}/${id}/`);

    return response.data;
  },

  async lookupBooks(query: string, take = 10): Promise<BookLookupItem[]> {
    const response = await httpClient.get<BookLookupItem[]>(`${BOOKS_BASE_PATH}/lookup/`, {
      params: {
        query: query.trim() || undefined,
        take,
      },
    });

    return response.data;
  },

  async createBook(payload: CreateBookRequest): Promise<string> {
    const response = await httpClient.post<string>(BOOKS_BASE_PATH, {
      title: payload.title.trim(),
      author: payload.author.trim(),
      genre: payload.genre.trim(),
      description: payload.description?.trim() || undefined,
      publisher: payload.publisher?.trim() || undefined,
      publishedOn: payload.publishedOn || undefined,
      isbn: payload.isbn?.trim() || undefined,
    });

    return response.data;
  },
};
