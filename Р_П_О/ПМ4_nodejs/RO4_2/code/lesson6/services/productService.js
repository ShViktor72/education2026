const products = [
    { id: 1, name: "Laptop" },
    { id: 2, name: "Mouse" }
];


// Service работает с данными.
// Здесь нет req и res.

export function getAllProducts() {

    return products;
}