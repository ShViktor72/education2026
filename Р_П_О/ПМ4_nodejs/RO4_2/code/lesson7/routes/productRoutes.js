// Это карта нашего REST API.
import express from "express";

import {
    getProducts,
    getProductById,
    createProduct,
    updateProduct,
    deleteProduct
} from "../controllers/productController.js";


const router = express.Router();


// READ
// GET /api/products
router.get("/", getProducts);


// READ
// GET /api/products/2
router.get("/:id", getProductById);


// CREATE
// POST /api/products
router.post("/", createProduct);


// UPDATE
// PATCH /api/products/2
router.patch("/:id", updateProduct);


// DELETE
// DELETE /api/products/2
router.delete("/:id", deleteProduct);


export default router;