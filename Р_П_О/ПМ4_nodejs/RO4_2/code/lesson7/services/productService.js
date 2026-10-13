// Здесь находятся данные и CRUD-операции.

let products = [
    {
        id: 1,
        name: "Laptop",
        price: 350000
    },
    {
        id: 2,
        name: "Mouse",
        price: 8000
    }
];


// READ — получить все товары

export function getAllProducts() {

    return products;
}


// READ — получить один товар

export function findProductById(id) {

    return products.find(
        product => product.id === id
    );
}


// CREATE — создать товар

export function addProduct(data) {

    const product = {
        id: products.length + 1,
        ...data
    };

    products.push(product);

    return product;
}


// UPDATE — изменить товар

export function updateProductById(id, changes) {

    const product = findProductById(id);

    if (!product) {
        return null;
    }

    Object.assign(product, changes);

    return product;
}


// DELETE — удалить товар

export function deleteProductById(id) {

    const index = products.findIndex(
        product => product.id === id
    );

    if (index === -1) {
        return null;
    }

    const [deletedProduct] =
        products.splice(index, 1);

    return deletedProduct;
}