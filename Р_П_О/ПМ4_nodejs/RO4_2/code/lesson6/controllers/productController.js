import { getAllProducts } from "../services/productService.js";


// Controller получает req и res
// и вызывает service

export function getProducts(req, res) {

    const products = getAllProducts();

    res.json(products);
}