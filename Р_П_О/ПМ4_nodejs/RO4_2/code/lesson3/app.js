import express from "express";

const app = express();

app.use(express.json());


// 1. Параметр маршрута
// GET /api/products/5
app.get("/api/products/:id", (req, res) => {

    console.log(req.params.id);

    res.send(`ID товара: ${req.params.id}`);
});


// 2. Query-параметр
// GET /api/products?category=laptops
app.get("/api/products", (req, res) => {

    console.log(req.query.category);

    res.send(`Категория: ${req.query.category}`);
});


// 3. Тело запроса
// POST /api/products
//
// JSON:
// {
//   "name": "Laptop",
//   "price": 350000
// }

app.post("/api/products", (req, res) => {

    console.log(req.body);

    res.json(req.body);
});


app.listen(3000, () => {
    console.log("Server started: http://localhost:3000");
});