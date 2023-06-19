CREATE TABLE raw.dataset_file
(
    registry_id VARCHAR(40) NOT NULL,
    file_name VARCHAR(40) NOT NULL,
    change_date TIMESTAMP NOT NULL,
    PRIMARY KEY (registry_id, file_name)
);

CREATE INDEX dataset_file_registry_id_fileName_idx ON raw.dataset_file(registry_id, file_name);

CREATE INDEX dataset_file_change_date_idx ON raw.dataset_file(change_date);
