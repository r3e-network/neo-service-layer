# Neo Service Layer API Pagination

## Overview

The Neo Service Layer API implements pagination for endpoints that return multiple items. This document describes how pagination works and how to use it in your applications.

## Pagination Parameters

For endpoints that return multiple items, the Neo Service Layer API supports pagination through the following query parameters:

- **page**: The page number to retrieve (default: 1).
- **per_page**: The number of items per page (default: 10, max: 100).

Example:

```http
GET /api/v1/events/subscriptions?page=2&per_page=20 HTTP/1.1
Host: api.neoservicelayer.org
X-API-Key: your-api-key
```

## Pagination Response

Pagination information is included in the response metadata:

```json
{
  "success": true,
  "data": [
    // Response data
  ],
  "error": null,
  "meta": {
    "requestId": "request-id",
    "timestamp": "2023-01-01T00:00:00Z",
    "pagination": {
      "page": 2,
      "per_page": 20,
      "total_pages": 5,
      "total_items": 100
    }
  }
}
```

The pagination metadata includes the following fields:

- **page**: The current page number.
- **per_page**: The number of items per page.
- **total_pages**: The total number of pages.
- **total_items**: The total number of items.

## Navigating Pages

To navigate through pages, update the `page` parameter in your request:

- **First page**: `page=1`
- **Next page**: `page=current_page + 1`
- **Previous page**: `page=current_page - 1`
- **Last page**: `page=total_pages`

Example:

```javascript
// Get the first page
fetch('https://api.neoservicelayer.org/api/v1/events/subscriptions?page=1&per_page=20', {
  headers: {
    'X-API-Key': 'your-api-key'
  }
})
.then(response => response.json())
.then(data => {
  console.log(data);
  
  // Get the next page
  const nextPage = data.meta.pagination.page + 1;
  if (nextPage <= data.meta.pagination.total_pages) {
    return fetch(`https://api.neoservicelayer.org/api/v1/events/subscriptions?page=${nextPage}&per_page=20`, {
      headers: {
        'X-API-Key': 'your-api-key'
      }
    });
  }
})
.then(response => response.json())
.then(data => {
  console.log(data);
});
```

## Handling Pagination in Applications

When handling pagination in your applications, you should implement a strategy to fetch all pages or allow users to navigate through pages:

### Example: Fetching All Pages

```javascript
async function fetchAllPages(url, options) {
  const allItems = [];
  let page = 1;
  let totalPages = 1;
  
  do {
    const response = await fetch(`${url}?page=${page}&per_page=100`, options);
    const data = await response.json();
    
    allItems.push(...data.data);
    
    totalPages = data.meta.pagination.total_pages;
    page++;
  } while (page <= totalPages);
  
  return allItems;
}
```

### Example: Implementing Pagination Controls

```javascript
function renderPaginationControls(pagination) {
  const { page, total_pages } = pagination;
  
  const controls = document.createElement('div');
  controls.className = 'pagination-controls';
  
  // First page button
  const firstPageButton = document.createElement('button');
  firstPageButton.textContent = 'First';
  firstPageButton.disabled = page === 1;
  firstPageButton.onclick = () => fetchPage(1);
  controls.appendChild(firstPageButton);
  
  // Previous page button
  const prevPageButton = document.createElement('button');
  prevPageButton.textContent = 'Previous';
  prevPageButton.disabled = page === 1;
  prevPageButton.onclick = () => fetchPage(page - 1);
  controls.appendChild(prevPageButton);
  
  // Page indicator
  const pageIndicator = document.createElement('span');
  pageIndicator.textContent = `Page ${page} of ${total_pages}`;
  controls.appendChild(pageIndicator);
  
  // Next page button
  const nextPageButton = document.createElement('button');
  nextPageButton.textContent = 'Next';
  nextPageButton.disabled = page === total_pages;
  nextPageButton.onclick = () => fetchPage(page + 1);
  controls.appendChild(nextPageButton);
  
  // Last page button
  const lastPageButton = document.createElement('button');
  lastPageButton.textContent = 'Last';
  lastPageButton.disabled = page === total_pages;
  lastPageButton.onclick = () => fetchPage(total_pages);
  controls.appendChild(lastPageButton);
  
  return controls;
}

async function fetchPage(page) {
  const response = await fetch(`https://api.neoservicelayer.org/api/v1/events/subscriptions?page=${page}&per_page=20`, {
    headers: {
      'X-API-Key': 'your-api-key'
    }
  });
  const data = await response.json();
  
  // Render items
  renderItems(data.data);
  
  // Render pagination controls
  const paginationControls = renderPaginationControls(data.meta.pagination);
  document.getElementById('pagination-container').innerHTML = '';
  document.getElementById('pagination-container').appendChild(paginationControls);
}
```

## Cursor-Based Pagination

For some endpoints, the Neo Service Layer API may use cursor-based pagination instead of page-based pagination. Cursor-based pagination is more efficient for large datasets and provides better performance.

With cursor-based pagination, you use a cursor to navigate through pages:

- **after**: A cursor that points to the item after which to start retrieving items.
- **before**: A cursor that points to the item before which to start retrieving items.
- **limit**: The number of items to retrieve (default: 10, max: 100).

Example:

```http
GET /api/v1/events/subscriptions?after=cursor&limit=20 HTTP/1.1
Host: api.neoservicelayer.org
X-API-Key: your-api-key
```

The response includes cursors for navigating to the next and previous pages:

```json
{
  "success": true,
  "data": [
    // Response data
  ],
  "error": null,
  "meta": {
    "requestId": "request-id",
    "timestamp": "2023-01-01T00:00:00Z",
    "pagination": {
      "has_next": true,
      "has_previous": true,
      "next_cursor": "next-cursor",
      "previous_cursor": "previous-cursor"
    }
  }
}
```

To navigate through pages with cursor-based pagination:

- **Next page**: Use the `next_cursor` as the `after` parameter.
- **Previous page**: Use the `previous_cursor` as the `before` parameter.

Example:

```javascript
// Get the first page
fetch('https://api.neoservicelayer.org/api/v1/events/subscriptions?limit=20', {
  headers: {
    'X-API-Key': 'your-api-key'
  }
})
.then(response => response.json())
.then(data => {
  console.log(data);
  
  // Get the next page
  if (data.meta.pagination.has_next) {
    return fetch(`https://api.neoservicelayer.org/api/v1/events/subscriptions?after=${data.meta.pagination.next_cursor}&limit=20`, {
      headers: {
        'X-API-Key': 'your-api-key'
      }
    });
  }
})
.then(response => response.json())
.then(data => {
  console.log(data);
});
```

## Best Practices

To effectively use pagination in your applications, follow these best practices:

1. **Use appropriate page sizes**: Use appropriate page sizes to balance between the number of requests and the amount of data per request.
2. **Cache results**: Cache results to avoid making unnecessary requests.
3. **Implement pagination controls**: Implement pagination controls to allow users to navigate through pages.
4. **Handle edge cases**: Handle edge cases such as empty pages and the last page.
5. **Use cursor-based pagination for large datasets**: Use cursor-based pagination for large datasets to improve performance.

## References

- [Neo Service Layer API](README.md)
- [Neo Service Layer API Endpoints](endpoints.md)
- [Neo Service Layer API Rate Limiting](rate-limiting.md)
