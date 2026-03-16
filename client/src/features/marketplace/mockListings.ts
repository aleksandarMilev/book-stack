import type { MarketplaceListing } from '@/types/marketplace.types';

export const marketplaceMockListings: MarketplaceListing[] = [
  {
    id: 'item1',
    titleKey: 'marketplace.mockListings.item1.title',
    authorKey: 'marketplace.mockListings.item1.author',
    cityKey: 'marketplace.mockListings.item1.city',
    genre: 'fiction',
    condition: 'likeNew',
    price: {
      primary: { amount: 29.9, currency: 'BGN' },
      secondary: { amount: 15.29, currency: 'EUR' },
    },
  },
  {
    id: 'item2',
    titleKey: 'marketplace.mockListings.item2.title',
    authorKey: 'marketplace.mockListings.item2.author',
    cityKey: 'marketplace.mockListings.item2.city',
    genre: 'nonfiction',
    condition: 'good',
    price: {
      primary: { amount: 22.5, currency: 'BGN' },
      secondary: { amount: 11.5, currency: 'EUR' },
    },
  },
  {
    id: 'item3',
    titleKey: 'marketplace.mockListings.item3.title',
    authorKey: 'marketplace.mockListings.item3.author',
    cityKey: 'marketplace.mockListings.item3.city',
    genre: 'science',
    condition: 'new',
    price: {
      primary: { amount: 41.9, currency: 'BGN' },
      secondary: { amount: 21.42, currency: 'EUR' },
    },
  },
  {
    id: 'item4',
    titleKey: 'marketplace.mockListings.item4.title',
    authorKey: 'marketplace.mockListings.item4.author',
    cityKey: 'marketplace.mockListings.item4.city',
    genre: 'poetry',
    condition: 'likeNew',
    price: {
      primary: { amount: 18.9, currency: 'BGN' },
      secondary: { amount: 9.66, currency: 'EUR' },
    },
  },
  {
    id: 'item5',
    titleKey: 'marketplace.mockListings.item5.title',
    authorKey: 'marketplace.mockListings.item5.author',
    cityKey: 'marketplace.mockListings.item5.city',
    genre: 'children',
    condition: 'good',
    price: {
      primary: { amount: 16.4, currency: 'BGN' },
      secondary: { amount: 8.39, currency: 'EUR' },
    },
  },
  {
    id: 'item6',
    titleKey: 'marketplace.mockListings.item6.title',
    authorKey: 'marketplace.mockListings.item6.author',
    cityKey: 'marketplace.mockListings.item6.city',
    genre: 'science',
    condition: 'acceptable',
    price: {
      primary: { amount: 12.9, currency: 'BGN' },
      secondary: { amount: 6.6, currency: 'EUR' },
    },
  },
];
