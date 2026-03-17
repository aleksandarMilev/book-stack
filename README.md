# BookStack Product Specification v1.1

## 1. Product summary

BookStack is a moderated online marketplace for books.

The platform allows:

- sellers to publish offers for books they own
- buyers to browse listings and place orders
- admins to moderate books and listings
- optional online payment or cash-on-delivery depending on seller-supported methods

BookStack is **not** a warehouse ecommerce store. It is a **marketplace** built around:

- a shared canonical book catalog
- seller-owned listings attached to canonical books
- moderated public visibility
- role-based access for buyers, sellers, and admins

The platform must optimize for:

- reliable listing moderation
- easy seller listing creation
- safe and clear buyer checkout
- correct visibility boundaries
- clean order, payment, and settlement lifecycle behavior

## 2. Business model

BookStack earns revenue by taking a **percentage commission from each completed sale**.

The platform supports two buyer payment methods:

- **Online payment**
- **Cash on delivery**

Both payment methods are supported in v1.

However, the platform must clearly separate:

1. how the buyer pays
2. who receives the money first
3. how BookStack earns and settles its fee

### 2.1 Online payment business model

For online payment orders:

- buyer pays through a BookStack-integrated payment provider
- BookStack records the successful payment
- BookStack calculates and stores:
  - gross amount
  - platform fee
  - seller net amount
- seller payout is tracked by the platform
- actual payout to seller may be manual/backoffice in v1

### 2.2 Cash on delivery business model

For cash-on-delivery orders:

- buyer pays on delivery
- money is collected outside the online provider flow
- seller and/or courier receives the money operationally
- BookStack still calculates and stores:
  - gross amount
  - platform fee
  - seller net amount
- the seller owes BookStack the commission through an operational settlement process

### 2.3 Fee snapshots

The applied fee must be stored as a snapshot on the transaction, not recalculated later from current settings.

Recommended stored fields:

- `GrossAmountEur`
- `PlatformFeePercent`
- `PlatformFeeAmountEur`
- `SellerNetAmountEur`

## 3. Business entities

### 3.1 User

A user is an authenticated identity in the platform.

A user may hold one or more capabilities:

- Buyer
- Seller
- Admin

A user is not automatically a seller just because they are authenticated.

### 3.2 User profile

A user profile stores general personal information for authenticated users.

Typical fields:

- first name
- last name
- profile image
- audit metadata

### 3.3 Seller profile

A seller profile is a separate business-facing profile attached to a user.

Purpose:

- represent seller capability
- store seller-specific operational and business data

Typical seller fields:

- seller display name
- phone number
- payment / settlement details for future use
- supported payment methods
- seller status
- audit metadata

A seller profile must exist before listings can be created.

### 3.4 Canonical book

A canonical book is the shared catalog record for a bibliographic book identity.

Purpose:

- prevent repeated re-entry of the same bibliographic data
- let multiple sellers attach listings to the same book
- provide a single approved metadata source for listings

Typical fields:

- title
- author
- genre
- description
- publisher
- published date
- ISBN
- normalized title
- normalized author
- normalized ISBN
- moderation status
- rejection reason
- audit and moderation metadata

Important rule:
A canonical book is **not directly sold**.

Important rule:
Canonical books are **not publicly browsed as a separate marketplace surface** in v1.

### 3.5 Canonical-book identity rules

If ISBN exists, canonical identity is primarily based on normalized ISBN.

If ISBN does not exist, likely duplicate detection uses normalized title + normalized author.

Publisher and published year may be used as moderator hints, not strict uniqueness truth.

Different translations are different canonical books.

Materially different editions are different canonical books.

### 3.6 Listing

A listing is a seller’s offer for a specific canonical book.

Typical listing fields:

- canonical book id
- seller id
- condition
- description
- quantity
- official price in EUR
- optional image
- moderation status
- rejection reason
- audit and moderation metadata

Important rules:

- multiple sellers may list the same canonical book
- the same seller may create multiple listings for the same canonical book
- editing listing data after approval requires reapproval before the new state becomes public

Recommended v1 implementation:

- if an approved listing is edited, it becomes non-public until reapproved

### 3.7 Order

An order is a buyer’s purchase record for one seller.

Core rule:
**One order belongs to exactly one seller.**

An order may contain multiple items, but all items must belong to the same seller.

Typical order fields:

- buyer user id if authenticated, otherwise null
- seller id
- checkout customer data
- shipping/contact details
- payment method
- money collection flow
- order status
- payment status
- settlement status
- total amount in EUR
- fee snapshot values
- audit timestamps

### 3.8 Order item

An order item is an immutable snapshot of purchased listing and book data at order creation time.

It stores:

- listing id
- canonical book id
- seller id
- title / author / genre / publisher / published date / ISBN snapshot
- listing description / image snapshot
- quantity
- condition
- unit price in EUR
- total price in EUR

### 3.9 Payment

A payment record stores payment-processing state associated with an order.

Typical fields:

- order id
- provider
- provider payment id
- amount in EUR
- payment status
- failure reason
- event linkage
- timestamps

Important rule:
Payment state is distinct from order state.

### 3.10 Payment webhook event

A payment webhook event stores raw provider event handling history.

Purpose:

- preserve provider event log
- support idempotency
- support reconciliation
- prevent duplicate event processing

## 4. Roles and permissions

### 4.1 Guest

Guest users can:

- browse public listings
- search and filter listings
- view listing details
- create orders
- choose supported payment method
- initiate guest-safe payment for guest-created orders

Guests cannot:

- create listings
- access profile
- access seller pages
- access admin pages

### 4.2 Buyer

A buyer is an authenticated user with standard user/profile capability.

A buyer can:

- do everything a guest can
- maintain profile
- see own orders
- use faster or prefilled checkout

A buyer is not automatically a seller.

### 4.3 Seller

A seller is an authenticated user with an approved or active seller profile.

A seller can:

- search for existing canonical books
- create listings attached to canonical books
- submit combined create-when-book-missing flows
- manage own listings
- see moderation state and rejection reasons for own books and listings
- see seller-scoped sold orders
- see only fulfillment-relevant buyer information for own orders

A seller cannot:

- moderate content
- access admin metrics
- see unrelated orders
- see other sellers’ private moderation data

### 4.4 Administrator

An admin is a user with admin role.

There may be multiple admins.

An admin can:

- review all books and listings
- approve
- reject with reason
- delete content as permitted
- view statistics
- inspect order, payment, and settlement state
- perform restricted operational overrides with audit trail

Only admins can moderate marketplace content.

## 5. Listing creation and canonical-book workflow

### 5.1 Seller listing creation

When seller wants to create a listing:

1. seller searches canonical books
2. if suitable canonical book exists, seller selects it
3. seller fills listing-specific fields
4. listing is created in pending moderation state

### 5.2 Combined create-when-book-missing flow

If no suitable canonical book exists:

1. seller enters extended bibliographic data plus listing-specific data in one workflow
2. system creates:
   - new canonical book in pending state
   - new listing in pending state
3. both require moderation
4. public visibility begins only after approval

Seller should not be forced through two unrelated manual creation flows.

## 6. Moderation rules

### 6.1 Canonical books

Possible states:

- PendingApproval
- Approved
- Rejected
- Deleted

### 6.2 Listings

Possible states:

- PendingApproval
- Approved
- Rejected
- Deleted

### 6.3 Edit-after-approval rule

If seller edits an approved listing:

- the edited state requires reapproval
- until approval, the edited listing is non-public in v1

## 7. Public browsing and discovery

Public users browse **approved listings only**.

Supported discovery behaviors:

- search by title
- author
- genre
- publisher
- ISBN
- price
- condition
- published date
- sorting and pagination

Public users do not browse canonical books as a separate marketplace surface in v1.

## 8. Order rules

### 8.1 Single-seller order rule

Each order may contain multiple items, but all items must belong to the same seller.

Cross-seller multi-item orders are not supported in v1.

### 8.2 Supported payment methods

At order creation, buyer chooses one of the payment methods supported by the seller:

- `Online`
- `CashOnDelivery`

### 8.3 Money collection flow

Money collection flow is distinct from buyer payment method.

Recommended values:

- `PlatformCollected`
- `SellerCollected`

Expected combinations:

- Online payment -> usually `PlatformCollected`
- Cash on delivery -> usually `SellerCollected`

## 9. Currency and pricing

### 9.1 Official currency

The official source-of-truth currency is **EUR**.

All official prices, totals, payment records, settlement values, and fee calculations are stored and processed in EUR.

### 9.2 BGN display

BGN is shown only as an informational equivalent for better UX and compliance needs.

The UI may display:

- EUR official price
- BGN equivalent

Example:

- `10.00 EUR / 19.56 BGN`

Important rule:
BGN display must never imply that BGN is the settlement currency when payment is actually in EUR.

## 10. Inventory and reservation rules

### 10.1 Reservation strategy

Stock is reserved at order creation.

This applies to:

- online payment orders
- cash-on-delivery orders

### 10.2 Online payment timeout

Unpaid online-payment orders expire after a short window.

Recommended v1 timeout:

- **20 minutes**

If payment is not successfully completed within the valid payable window:

- order becomes expired or cancelled according to implementation
- reserved stock is released

### 10.3 Payment retry

A single order may have multiple payment attempts, but:

- only one active pending payment attempt may exist at a time
- retry is allowed only while order is still payable
- successful payment ends payment retry eligibility

### 10.4 Source of truth

Payment webhook events are authoritative for provider outcome.

Frontend return page is informational only.

## 11. Status model

### 11.1 Order status

Recommended v1 statuses:

- `PendingPayment`
- `PendingConfirmation`
- `Confirmed`
- `Shipped`
- `Delivered`
- `Completed`
- `Cancelled`
- `Expired`

Meaning:

- `PendingPayment`: online-payment order created, awaiting payment
- `PendingConfirmation`: paid online or COD order exists, awaiting seller/admin processing
- `Confirmed`: accepted or ready for fulfillment
- `Shipped`: dispatched
- `Delivered`: delivered to buyer
- `Completed`: finalized and closed
- `Cancelled`: cancelled intentionally
- `Expired`: online payment window expired before payment success

### 11.2 Payment status

Recommended v1 statuses:

- `NotRequired`
- `Pending`
- `Paid`
- `Failed`
- `Expired`
- `Refunded`
- `Cancelled`

Meaning:

- `NotRequired`: COD order, no online payment required
- `Pending`: online payment started but not finalized
- `Paid`: online payment succeeded
- `Failed`: payment attempt failed
- `Expired`: payable window expired
- `Refunded`: refund recorded
- `Cancelled`: payment attempt cancelled

### 11.3 Settlement status

Recommended v1 statuses:

- `Pending`
- `Settled`
- `Waived`
- `Disputed`

Meaning:

- `Pending`: financial reconciliation is not yet resolved
- `Settled`: platform fee and seller side are financially resolved
- `Waived`: platform intentionally waives the fee or settlement
- `Disputed`: operational or financial dispute exists

## 12. Fee and settlement rules

### 12.1 Fee calculation

Use one configurable percentage fee in v1.

The fee is applied to the order gross amount in EUR and stored as a snapshot at order creation.

Store:

- `PlatformFeePercent`
- `GrossAmountEur`
- `PlatformFeeAmountEur`
- `SellerNetAmountEur`

### 12.2 When fee is earned

For online payment:

- platform fee is considered earned when payment succeeded and order is not later cancelled/refunded under refund rules

For cash on delivery:

- platform fee becomes due when order is marked `Delivered` or `Completed`

### 12.3 Online-payment settlement

For online payments:

- BookStack captures payment through provider
- BookStack fee is enforceable from collected funds
- seller payout may be manual in v1
- full payout automation is out of scope for v1

### 12.4 COD settlement

For COD:

- seller and/or courier collects money operationally
- BookStack still records fee due
- commission settlement is operational/manual in v1
- full automated seller commission collection is out of scope for v1

## 13. Who can change what

### 13.1 Seller manual actions

Recommended seller-allowed order transitions on own orders:

- `PendingConfirmation -> Confirmed`
- `Confirmed -> Shipped`
- `Shipped -> Delivered`

Seller cannot:

- mark online payment as paid
- refund payments
- access unrelated orders
- alter buyer identity/payment internals beyond allowed visibility

### 13.2 Admin manual actions

Admin may:

- inspect all orders
- adjust statuses operationally when necessary
- manually override payment state only as audited fallback
- record refund or operational cancellation if needed
- manage settlement status when required operationally

All admin overrides should be audited.

## 14. Seller visibility and privacy

Seller can see only the minimum information needed to fulfill their own orders.

For seller-owned orders and items, seller may see:

- buyer first name
- buyer last name
- delivery name
- phone number
- email
- delivery address
- city
- postal code
- country
- ordered item details
- order status
- payment method
- payment status at business level
- gross amount
- platform fee
- expected seller net

Seller cannot see:

- payment provider internals
- guest payment token
- admin-only notes
- unrelated orders
- unrelated buyer data

## 15. Public visibility rules

Public users may see only:

- approved listings
- public-safe bibliographic and listing details derived from approved content

Public users may not see:

- pending or rejected items
- moderation reasons
- seller-private data
- admin-private data

Canonical books are not publicly browsed as a standalone page in v1.

## 16. Admin rules

There may be multiple admins.

Admin role is role-based, not singleton-based.

Admin features include:

- moderation of books and listings
- statistics dashboard
- order/payment/settlement operational visibility
- controlled destructive actions
- auditability of sensitive operations

## 17. Images and file handling

### 17.1 Product rules

- listings and profiles may have images
- replacing image should preserve correct old/new behavior
- deleting entity should not accidentally delete unrelated/shared/default images

### 17.2 Production storage recommendation

Production should use object storage, not only local filesystem.

Recommended options:

- S3-compatible storage
- Azure Blob Storage
- Cloudflare R2

### 17.3 Deletion semantics

Recommended:

- replacing image may delete previous image if entity no longer references it
- soft delete should not require immediate physical deletion
- hard delete may trigger immediate deletion if safe
- periodic orphan cleanup job is recommended

## 18. Security and trust assumptions

The system must assume:

- frontend role state is UX-only
- backend enforces all authorization
- guest payment access is token-protected
- provider webhooks are idempotent and authoritative
- moderation is admin-only
- seller order visibility is seller-scoped only

High-risk areas:

- payment retries and replays
- reservation exhaustion
- guest token misuse
- image/file abuse
- seller access to buyer data
- state transition inconsistencies
- manual admin override misuse

## 19. Product quality goals

The shipped product should be:

- reliable
- moderated
- mobile-friendly and desktop-friendly
- clear about listing moderation state
- clear about order, payment, and settlement state
- safe against common abuse
- consistent in business rules
- operationally supportable

## 20. Non-goals for v1

The following are not required for v1 unless later chosen:

- multi-seller single order
- automatic seller payouts and fee settlement
- public canonical-book browse catalog
- advanced bibliographic merge system
- fully automated refund engine
- complex many-to-many genre taxonomy
- marketplace-wide cross-seller unified checkout

## 21. Future extension points

Possible future enhancements:

- grouped multi-seller cart
- automated payouts
- richer seller onboarding/KYC
- canonical-book merge tools
- multi-genre taxonomy
- saved favorites/wishlists
- messaging between buyer and seller
- richer delivery integrations
