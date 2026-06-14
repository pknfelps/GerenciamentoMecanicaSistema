CREATE TABLE users (
    id UUID PRIMARY KEY,
    name VARCHAR(100) NOT NULL,
    password VARCHAR(100) NOT NULL,
    role VARCHAR(100) NOT NULL
);

INSERT INTO users(id, name, password, role)
VALUES ('de0ec153-5578-430e-b530-96e4118f1721', 'Admin', 'Admin@123', 'Admin');

CREATE TABLE customers (
    id UUID PRIMARY KEY,
    name VARCHAR(100) NOT NULL,
    document VARCHAR(100) NOT NULL UNIQUE,
    phone VARCHAR(100) NOT NULL,
    email VARCHAR(100) NOT NULL
);

CREATE TABLE vehicles (
    id UUID PRIMARY KEY,
    customer_document VARCHAR(100) NOT NULL REFERENCES customers(document),
    brand VARCHAR(100) NOT NULL,
    model VARCHAR(100) NOT NULL,
    year INT NOT NULL,
    license_plate VARCHAR(7) NOT NULL UNIQUE
);

CREATE TABLE orders (
    id UUID PRIMARY KEY,
    customer_document VARCHAR(100) NOT NULL REFERENCES customers(document),
    vehicle_license_plate VARCHAR(100) NOT NULL REFERENCES vehicles(license_plate),
    budget DOUBLE PRECISION NOT NULL,
    status VARCHAR(50) NOT NULL,
    date_created TIMESTAMP NOT NULL DEFAULT NOW(),
    date_finished TIMESTAMP NOT NULL
);

CREATE TABLE stock (
    id UUID PRIMARY KEY,
    name VARCHAR(255) NOT NULL,
    brand VARCHAR(255) NOT NULL,
    price DOUBLE PRECISION NOT NULL,
    amount INT NOT NULL,
    reserved_amount INT NOT NULL DEFAULT 0
);

CREATE TABLE services (
    id UUID PRIMARY KEY,
    description VARCHAR(255) NOT NULL,
    hours FLOAT NOT NULL,
    price_per_hour DOUBLE PRECISION NOT NULL
);

CREATE TABLE order_items (
    id UUID NOT NULL REFERENCES stock(id),
    order_id UUID NOT NULL REFERENCES orders(id),
    name VARCHAR(255) NOT NULL,
    brand VARCHAR(255) NOT NULL,
    price DOUBLE PRECISION NOT NULL,
    amount INT NOT NULL
);

CREATE TABLE order_services (
    id UUID NOT NULL REFERENCES services(id),
    order_id UUID NOT NULL REFERENCES orders(id),
    description VARCHAR(255) NOT NULL,
    hours FLOAT NOT NULL,
    price_per_hour DOUBLE PRECISION NOT NULL,
    amount INT NOT NULL
);