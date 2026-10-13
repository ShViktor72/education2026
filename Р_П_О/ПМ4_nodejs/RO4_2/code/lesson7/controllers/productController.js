// Controller связывает HTTP и Service.
import {
    getAllProducts,
    findProductById,
    addProduct,
    updateProductById,
    deleteProductById
} from "../services/productService.js";


// ----------------------------------------------------
// READ — получить все товары
// ----------------------------------------------------

export function getProducts(req, res) {

    const products = getAllProducts();

    res.json(products);
}


// ----------------------------------------------------
// READ — получить один товар
// ----------------------------------------------------

export function getProductById(req, res) {

    // id приходит из URL:
    // /api/products/2

    const id = Number(req.params.id);


    const product = findProductById(id);


    if (!product) {

        return res.status(404).json({
            message: "Product not found"
        });
    }


    res.json(product);
}


// ----------------------------------------------------
// CREATE — создать товар
// ----------------------------------------------------

export function createProduct(req, res) {

    // Данные нового товара находятся
    // в req.body

    const product = addProduct(req.body);


    res.status(201).json(product);
}


// ----------------------------------------------------
// UPDATE — изменить товар
// ----------------------------------------------------

export function updateProduct(req, res) {

    // Какой товар изменить?
    const id = Number(req.params.id);

    // Что изменить?
    const changes = req.body;


    const product =
        updateProductById(id, changes);


    if (!product) {

        return res.status(404).json({
            message: "Product not found"
        });
    }


    res.json(product);
}


// ----------------------------------------------------
// DELETE — удалить товар
// ----------------------------------------------------

export function deleteProduct(req, res) {

    const id = Number(req.params.id);


    const product =
        deleteProductById(id);


    if (!product) {

        return res.status(404).json({
            message: "Product not found"
        });
    }


    res.json(product);
}