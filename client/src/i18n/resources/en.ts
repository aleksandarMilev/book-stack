const en = {
  common: {
    appName: 'BookStack',
    language: 'Language',
    actions: {
      browseMarketplace: 'Browse marketplace',
      discoverBooks: 'Discover books',
      clearFilters: 'Clear filters',
      learnMore: 'Learn more',
      viewListing: 'View listing',
      openFilters: 'Open filters',
      close: 'Close',
    },
    labels: {
      comingSoon: 'Coming soon',
      premiumSelection: 'Premium selection',
      trustedMarketplace: 'Trusted marketplace',
    },
  },
  nav: {
    primary: {
      home: 'Home',
      marketplace: 'Marketplace',
      books: 'Books',
    },
    account: {
      login: 'Login',
      register: 'Register',
      profile: 'Profile',
      myOrders: 'My orders',
    },
    seller: {
      myListings: 'My listings',
      soldOrders: 'Sold orders',
    },
    admin: {
      dashboard: 'Dashboard',
      books: 'Books moderation',
      listings: 'Listings moderation',
    },
    mobile: {
      openMenu: 'Open menu',
      closeMenu: 'Close menu',
    },
  },
  taxonomy: {
    genres: {
      fiction: 'Fiction',
      nonfiction: 'Non-fiction',
      children: 'Children',
      science: 'Science',
      poetry: 'Poetry',
    },
    conditions: {
      all: 'Any condition',
      new: 'New',
      likeNew: 'Like new',
      good: 'Good',
      acceptable: 'Acceptable',
    },
    sort: {
      featured: 'Featured',
      priceAsc: 'Price: low to high',
      priceDesc: 'Price: high to low',
      newest: 'Newest first',
    },
  },
  shell: {
    accountArea: 'Account',
    sellerArea: 'Seller',
    adminArea: 'Admin',
    footerTagline:
      'A calm and trusted marketplace where readers, collectors, and sellers meet around quality books.',
    footerCopyright: 'BookStack',
  },
  home: {
    hero: {
      eyebrow: 'Curated books, fair deals, trusted community',
      title: 'Buy and sell books with confidence',
      description:
        'BookStack helps readers discover quality titles and gives sellers a clear, professional way to list, ship, and get paid.',
      primaryCta: 'Browse marketplace',
      secondaryCta: 'How it works',
      statOne: 'Curated listings',
      statTwo: 'Secure checkout flow',
      statThree: 'Support for buyers and sellers',
    },
    value: {
      title: 'Built for both buyers and sellers',
      description:
        'The platform is designed to keep browsing smooth, listing fast, and transactions transparent from start to finish.',
      buyersTitle: 'For buyers',
      buyersDescription:
        'Search by genre, compare listing quality quickly, and order with clear payment flow and reliable updates.',
      sellersTitle: 'For sellers',
      sellersDescription:
        'Create listings in minutes, keep inventory visible, and track sold orders with practical seller tools.',
    },
    genres: {
      title: 'Featured genres',
      description: 'Explore popular categories and find your next favorite read or your next buyer.',
      fictionDescription: 'Novels, stories, and contemporary classics.',
      nonfictionDescription: 'Biographies, business, history, and ideas.',
      childrenDescription: 'Picture books and stories for young readers.',
      scienceDescription: 'Curious minds, discoveries, and practical science.',
      poetryDescription: 'Voices, rhythm, and timeless collections.',
    },
    how: {
      title: 'How it works',
      description: 'Simple flows that keep the experience clear on desktop and mobile.',
      stepOneTitle: 'Browse or publish',
      stepOneDescription: 'Buyers explore listings while sellers add books with clear details and pricing.',
      stepTwoTitle: 'Order and pay',
      stepTwoDescription: 'Guests and authenticated users can complete checkout through a secure payment session.',
      stepThreeTitle: 'Track and manage',
      stepThreeDescription:
        'Buyers follow order status and sellers monitor sold orders from one organized workspace.',
    },
  },
  marketplace: {
    title: 'Marketplace',
    subtitle: 'Search listings, compare quality, and find books worth keeping.',
    searchLabel: 'Search listings',
    searchPlaceholder: 'Search by title or author',
    sortLabel: 'Sort',
    filtersTitle: 'Filters',
    genreLabel: 'Genre',
    conditionLabel: 'Condition',
    desktopFiltersTitle: 'Refine results',
    mobileFiltersTitle: 'Filters',
    loadingTitle: 'Loading marketplace',
    loadingDescription: 'Preparing listing cards and filters.',
    emptyTitle: 'No listings matched your filters',
    emptyDescription: 'Try a different search or reset your filters to see more books.',
    resultsCount: '{{count}} listings',
    mockListings: {
      item1: {
        title: 'The Silent Library',
        author: 'Mira Dane',
        city: 'Sofia',
      },
      item2: {
        title: 'Ocean of Small Things',
        author: 'Kalin Petrov',
        city: 'Plovdiv',
      },
      item3: {
        title: 'The Practical Botanist',
        author: 'Elena Moore',
        city: 'Varna',
      },
      item4: {
        title: 'Letters to Tomorrow',
        author: 'Nikolai Voss',
        city: 'Burgas',
      },
      item5: {
        title: 'Bright Constellations',
        author: 'Ivana Reed',
        city: 'Ruse',
      },
      item6: {
        title: 'The Mountain Atlas',
        author: 'Stefan Iliev',
        city: 'Veliko Tarnovo',
      },
    },
  },
  pages: {
    listingDetails: {
      title: 'Listing details',
      description: 'Detailed listing view will be connected in the next step.',
      listingId: 'Listing ID: {{id}}',
    },
    books: {
      title: 'Books',
      description: 'Books catalog and discovery tools will be expanded in the next step.',
    },
    login: {
      title: 'Login',
      description: 'Authentication form and validation are planned next.',
    },
    register: {
      title: 'Register',
      description: 'Account registration flow will be added in the next step.',
    },
    profile: {
      title: 'Profile',
      description: 'User profile settings and account details will be connected next.',
    },
    myListings: {
      title: 'My listings',
      description: 'Seller listing management tools will be implemented next.',
    },
    myOrders: {
      title: 'My orders',
      description: 'Order history and status tracking will be added in the next step.',
    },
    sellerSoldOrders: {
      title: 'Seller sold orders',
      description: 'Sold order management for sellers will be integrated next.',
    },
    paymentReturn: {
      title: 'Payment return',
      description: 'Payment return handling and status resolution will be connected next.',
    },
    adminDashboard: {
      title: 'Admin dashboard',
      description: 'Moderation overview and statistics widgets will be added next.',
    },
    adminBooks: {
      title: 'Admin books moderation',
      description: 'Book moderation queue and actions will be integrated next.',
    },
    adminListings: {
      title: 'Admin listings moderation',
      description: 'Listing moderation queue and actions will be integrated next.',
    },
    notFound: {
      title: 'Page not found',
      description: 'The page you are looking for does not exist or was moved.',
      action: 'Go to home',
    },
  },
} as const;

export default en;
