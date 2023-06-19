CREATE TABLE rosstat.bankruptcy_prediction
(
    hash_id UUID NOT NULL,
    parent_hash_id UUID NOT NULL,
    model VARCHAR(20) NOT NULL,
    score DECIMAL(18, 2) NOT NULL,
    probability VARCHAR(6) NOT NULL,
    change_date TIMESTAMP NOT NULL,
    PRIMARY KEY (hash_id),
    FOREIGN KEY (parent_hash_id) REFERENCES rosstat.report (hash_id)
);

CREATE INDEX bankruptcy_prediction_hash_id_idx on rosstat.bankruptcy_prediction(hash_id);

CREATE INDEX bankruptcy_prediction_parent_hash_id_idx on rosstat.bankruptcy_prediction(parent_hash_id);
