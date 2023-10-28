;; invoke: lts
;; expect: (i32:1)

(module
    (func (export "lts") (result i32)
        i64.const -10
        i64.const 42
        i64.lt_s
    )
)