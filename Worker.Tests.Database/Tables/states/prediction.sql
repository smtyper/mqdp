CREATE TABLE states.prediction
(
    hash_id UUID NOT NULL,
    change_date TIMESTAMP NOT NULL,
    is_in_processing BOOLEAN NOT NULL,
    PRIMARY KEY (hash_id)
);

CREATE INDEX prediction_key_idx on states.prediction(hash_id);

CREATE INDEX prediction_status_idx on states.prediction(is_in_processing);
