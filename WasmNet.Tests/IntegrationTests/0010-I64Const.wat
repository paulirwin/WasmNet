;; invoke: i64const
;; expect: (i64:4200000000)

(module
    (func (export "i64const") (result i64)
        (i64.const 4200000000)
    )
)