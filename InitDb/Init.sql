CREATE TABLE usuarios (
    id UUID PRIMARY KEY,
    nome VARCHAR(100) NOT NULL,
    senha VARCHAR(100) NOT NULL,
    cargo VARCHAR(100) NOT NULL
);

INSERT INTO usuarios(id, nome, senha, cargo)
VALUES ('de0ec153-5578-430e-b530-96e4118f1721', 'Admin', 'Admin@123', 'Admin');

CREATE TABLE stock (
    id SERIAL PRIMARY KEY,
    item_name VARCHAR(255) NOT NULL,
    brand VARCHAR(255) NOT NULL,
    price DOUBLE PRECISION NOT NULL,
    amount INT NOT NULL,
    reserved_amount INT NOT NULL DEFAULT 0
);

CREATE TABLE clientes (
    id UUID PRIMARY KEY,
    nome VARCHAR(100) NOT NULL,
    documento VARCHAR(100) NOT NULL,
    celular VARCHAR(100) NOT NULL,
    email VARCHAR(100) NOT NULL
);

CREATE TABLE vehicles (
    id UUID PRIMARY KEY,
    brand VARCHAR(100) NOT NULL,
    model VARCHAR(100) NOT NULL,
    year INT NOT NULL,
    license_plate VARCHAR(100) NOT NULL
);