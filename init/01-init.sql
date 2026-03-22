CREATE EXTENSION IF NOT EXISTS timescaledb;

CREATE TABLE IF NOT EXISTS posiciones_agregadas (
    time_utc        TIMESTAMPTZ    NOT NULL,
    symbol          TEXT           NOT NULL,
    count           INTEGER        NOT NULL,
    average_price   NUMERIC(18,8)  NOT NULL,
    total_quantity  NUMERIC(18,8)  NOT NULL,
    window_start    TIMESTAMPTZ    NOT NULL,
    window_end      TIMESTAMPTZ    NOT NULL,
    PRIMARY KEY (symbol, time_utc)
);

SELECT create_hypertable('posiciones_agregadas', 'time_utc', if_not_exists => TRUE);

CREATE INDEX IF NOT EXISTS idx_posiciones_agregadas_time_desc
ON posiciones_agregadas (time_utc DESC);

CREATE TABLE IF NOT EXISTS alertas_precio (
    id                  BIGSERIAL      PRIMARY KEY,
    created_at          TIMESTAMPTZ    NOT NULL DEFAULT NOW(),
    symbol              TEXT           NOT NULL,
    previous_time_utc   TIMESTAMPTZ    NOT NULL,
    current_time_utc    TIMESTAMPTZ    NOT NULL,
    previous_avg_price  NUMERIC(18,8)  NOT NULL,
    current_avg_price   NUMERIC(18,8)  NOT NULL,
    percentage_change   NUMERIC(10,4)  NOT NULL,
    direction           TEXT           NOT NULL CHECK (direction IN ('UP', 'DOWN'))
);

CREATE INDEX IF NOT EXISTS idx_alertas_precio_symbol_created
ON alertas_precio (symbol, created_at DESC);

CREATE INDEX IF NOT EXISTS idx_alertas_precio_current_time
ON alertas_precio (current_time_utc DESC);