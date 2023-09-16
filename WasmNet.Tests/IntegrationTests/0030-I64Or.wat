;; invoke: or
;; expect: (i64:1002)

(module
    (func (export "or") (result i64)
        i64.const 42
        i64.const 1000
        i64.or
    )
)