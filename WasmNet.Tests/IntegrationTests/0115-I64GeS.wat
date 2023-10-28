;; invoke: ges
;; expect: (i32:1)

(module
    (func (export "ges") (result i32)
        i64.const -42
        i64.const -42
        i64.ge_s
    )
)