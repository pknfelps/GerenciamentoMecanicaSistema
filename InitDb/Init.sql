CREATE TABLE clientes (
    id UUID PRIMARY KEY,
    nome VARCHAR(100),
    documento VARCHAR(100),
    celular VARCHAR(100),
    email VARCHAR(100)
);

CREATE TABLE usuarios (
    id UUID PRIMARY KEY,
    nome VARCHAR(100),
    senha VARCHAR(100),
    cargo VARCHAR(100)
);

INSERT INTO usuarios(id, nome, senha, cargo)
VALUES ('de0ec153-5578-430e-b530-96e4118f1721', 'Admin', 'Admin@123', 'Admin');