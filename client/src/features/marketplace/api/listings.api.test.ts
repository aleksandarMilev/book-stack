import { afterEach, describe, expect, it, vi } from 'vitest';

import { httpClient } from '@/api/httpClient';
import { listingsApi } from '@/features/marketplace/api/listings.api';

vi.mock('@/api/httpClient', () => ({
  httpClient: {
    get: vi.fn(),
    post: vi.fn(),
    put: vi.fn(),
    delete: vi.fn(),
  },
}));

describe('listingsApi form-data request contracts', () => {
  afterEach(() => {
    vi.clearAllMocks();
  });

  it('createListing sends multipart form-data with expected field names', async () => {
    vi.mocked(httpClient.post).mockResolvedValue({ data: 'listing-1' });

    const createdListingId = await listingsApi.createListing({
      bookId: 'book-1',
      price: 19.99,
      currency: ' eur ',
      condition: 'veryGood',
      quantity: 2,
      description: '  A clean listing description.  ',
      image: null,
      removeImage: false,
    });

    const postCallArguments = vi.mocked(httpClient.post).mock.calls[0];
    expect(postCallArguments).toBeDefined();
    if (!postCallArguments) {
      throw new Error('Expected create listing request payload to be posted.');
    }

    const [url, payload, config] = postCallArguments;
    expect(url).toBe('/BookListings');
    expect(payload).toBeInstanceOf(FormData);
    expect(config).toEqual({
      headers: { 'Content-Type': 'multipart/form-data' },
    });

    const formData = payload as FormData;
    expect(formData.get('bookId')).toBe('book-1');
    expect(formData.get('price')).toBe('19.99');
    expect(formData.get('currency')).toBe('EUR');
    expect(formData.get('condition')).toBe('2');
    expect(formData.get('quantity')).toBe('2');
    expect(formData.get('description')).toBe('A clean listing description.');
    expect(formData.get('removeImage')).toBe('false');
    expect(formData.get('image')).toBeNull();
    expect(createdListingId).toBe('listing-1');
  });

  it('createListingWithBook sends multipart form-data and omits empty optional fields', async () => {
    vi.mocked(httpClient.post).mockResolvedValue({ data: 'listing-2' });

    await listingsApi.createListingWithBook({
      title: '  Missing Book  ',
      author: '  New Author  ',
      genre: '  Fantasy  ',
      bookDescription: ' ',
      publisher: ' ',
      isbn: ' ',
      price: 14.5,
      currency: 'eur',
      condition: 'good',
      quantity: 1,
      description: '  Created with missing-book flow.  ',
      image: null,
    });

    const postCallArguments = vi.mocked(httpClient.post).mock.calls[0];
    expect(postCallArguments).toBeDefined();
    if (!postCallArguments) {
      throw new Error('Expected create-with-book request payload to be posted.');
    }

    const [url, payload, config] = postCallArguments;
    expect(url).toBe('/BookListings/with-book/');
    expect(payload).toBeInstanceOf(FormData);
    expect(config).toEqual({
      headers: { 'Content-Type': 'multipart/form-data' },
    });

    const formData = payload as FormData;
    expect(formData.get('title')).toBe('Missing Book');
    expect(formData.get('author')).toBe('New Author');
    expect(formData.get('genre')).toBe('Fantasy');
    expect(formData.get('price')).toBe('14.5');
    expect(formData.get('currency')).toBe('EUR');
    expect(formData.get('condition')).toBe('3');
    expect(formData.get('quantity')).toBe('1');
    expect(formData.get('description')).toBe('Created with missing-book flow.');
    expect(formData.get('bookDescription')).toBeNull();
    expect(formData.get('publisher')).toBeNull();
    expect(formData.get('publishedOn')).toBeNull();
    expect(formData.get('isbn')).toBeNull();
    expect(formData.get('image')).toBeNull();
  });

  it('editListing sends multipart form-data with removeImage and image fields', async () => {
    vi.mocked(httpClient.put).mockResolvedValue({ data: undefined });
    const image = new File(['listing'], 'listing.png', { type: 'image/png' });

    await listingsApi.editListing('listing-1', {
      bookId: 'book-1',
      price: 11,
      currency: 'EUR',
      condition: 'acceptable',
      quantity: 4,
      description: 'Edited listing description',
      image,
      removeImage: true,
    });

    const putCallArguments = vi.mocked(httpClient.put).mock.calls[0];
    expect(putCallArguments).toBeDefined();
    if (!putCallArguments) {
      throw new Error('Expected edit listing request payload to be posted.');
    }

    const [url, payload, config] = putCallArguments;
    expect(url).toBe('/BookListings/listing-1/');
    expect(payload).toBeInstanceOf(FormData);
    expect(config).toEqual({
      headers: { 'Content-Type': 'multipart/form-data' },
    });

    const formData = payload as FormData;
    expect(formData.get('bookId')).toBe('book-1');
    expect(formData.get('condition')).toBe('4');
    expect(formData.get('removeImage')).toBe('true');
    expect(formData.get('image')).toBe(image);
  });
});
