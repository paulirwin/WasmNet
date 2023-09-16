;; invoke: and
;; expect: (i64:8)

(module
    (func (export "and") (result i64)
        i64.const 42
        i64.const 8
        i64.and
    )
)