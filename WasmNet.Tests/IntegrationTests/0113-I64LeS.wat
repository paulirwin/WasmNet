;; invoke: les
;; expect: (i32:1)

(module
    (func (export "les") (result i32)
        i64.const -42
        i64.const -42
        i64.le_s
    )
)