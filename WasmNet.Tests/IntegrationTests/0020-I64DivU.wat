;; invoke: div
;; expect: (i64:1)

(module
    (func (export "div") (result i64)
        i64.const 6
        i64.const 4
        i64.div_u
    )
)