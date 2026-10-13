import express from "express";

const router = express.Router();


// GET /api/products

router.get("/", (req, res) => {

    res.send("Список товаров");

});


// POST /api/products

router.post("/", (req, res) => {

    res.send("Создание товара");

});


// GET /api/products/5

router.get("/:id", (req, res) => {

    res.send(`Товар ${req.params.id}`);

});


export default router;