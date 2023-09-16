;; invoke: shrs
;; expect: (i64:-11)

(module
    (func (export "shrs") (result i64)
        i64.const -42
        i64.const 2
        i64.shr_s
    )
)