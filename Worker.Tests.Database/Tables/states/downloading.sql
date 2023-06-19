CREATE TABLE states.downloading
(
    registry_id VARCHAR(40) NOT NULL,
    file_name VARCHAR(40) NOT NULL,
    PRIMARY KEY (registry_id, file_name)
);

CREATE INDEX downloading_key_idx on states.downloading(registry_id, file_name);
