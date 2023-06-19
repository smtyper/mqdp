CREATE TABLE rosstat.report
(
    hash_id UUID NOT NULL,
    name VARCHAR(1000) NULL,
    okpo VARCHAR(8) NULL,
    okopf VARCHAR(5) NULL,
    okfs VARCHAR(2) NULL,
    okved VARCHAR(8) NULL,
    inn VARCHAR(100) NULL,
    change_date DATE NOT NULL,
    type VARCHAR(100) NULL,
    period INT NOT NULL,
    data_date DATE NOT NULL,
    values VARCHAR NULL,
    PRIMARY KEY (hash_id)
);

CREATE INDEX report_hash_id_idx on rosstat.report(hash_id);

CREATE INDEX report_change_date_idx on rosstat.report(change_date);
