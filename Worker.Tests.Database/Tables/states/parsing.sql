CREATE TABLE states.parsing
(
    registry_id VARCHAR(40) NOT NULL,
    file_name VARCHAR(40) NOT NULL,
    change_date TIMESTAMP NOT NULL,
    is_in_processing BOOLEAN NOT NULL,
    PRIMARY KEY (registry_id, file_name)
);

CREATE INDEX parsing_key_idx on states.parsing(registry_id, file_name);
CREATE INDEX parsing_status_idx on states.parsing(is_in_processing);
